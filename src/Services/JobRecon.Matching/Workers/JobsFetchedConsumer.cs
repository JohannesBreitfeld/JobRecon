using System.Text;
using System.Text.Json;
using JobRecon.Contracts.Events;
using JobRecon.Matching.Clients;
using JobRecon.Matching.Configuration;
using JobRecon.Matching.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JobRecon.Matching.Workers;

public sealed class JobsFetchedConsumer : BackgroundService
{
    private const string QueueName = "matching.jobs-fetched";
    private const string RoutingKey = "jobs.fetched";
    private const string RetryCountHeader = "x-retry-count";
    private const int MaxRetries = 3;

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobsFetchedConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public JobsFetchedConsumer(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<JobsFetchedConsumer> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeRabbitMqAsync(stoppingToken);

        if (_channel is null)
        {
            _logger.LogWarning("RabbitMQ channel not initialized, jobs-fetched consumer will not run");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var fetchedEvent = JsonSerializer.Deserialize<JobsFetchedIntegrationEvent>(message);

                if (fetchedEvent is not null && fetchedEvent.NewJobCount > 0)
                {
                    _logger.LogInformation(
                        "Received JobsFetchedEvent: {NewCount} new jobs from source {SourceId}",
                        fetchedEvent.NewJobCount, fetchedEvent.JobSourceId);

                    await TriggerEmbeddingAsync(stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(ea.BasicProperties);
                if (retryCount < MaxRetries)
                {
                    _logger.LogWarning(ex,
                        "Error processing jobs-fetched message, retry {Attempt}/{Max}",
                        retryCount + 1, MaxRetries);

                    var props = new BasicProperties { Persistent = true };
                    props.Headers = new Dictionary<string, object?> { [RetryCountHeader] = (long)(retryCount + 1) };
                    await _channel.BasicPublishAsync(
                        _settings.Exchange, RoutingKey, true, props, ea.Body, stoppingToken);
                }
                else
                {
                    _logger.LogError(ex,
                        "jobs-fetched message exceeded max retries ({Max}), discarding", MaxRetries);
                }

                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Started consuming jobs-fetched events from queue {Queue}", QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task TriggerEmbeddingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobsClient = scope.ServiceProvider.GetRequiredService<IJobsClient>();
        var ollamaClient = scope.ServiceProvider.GetRequiredService<IOllamaClient>();
        var vectorStore = scope.ServiceProvider.GetRequiredService<IVectorStore>();

        await vectorStore.EnsureCollectionAsync(ct);

        var offset = 0;
        var embedded = 0;
        const int batchSize = 100;
        const int maxJobs = 2000;
        using var semaphore = new SemaphoreSlim(4);

        while (offset < maxJobs)
        {
            var jobsResponse = await jobsClient.GetActiveJobsAsync(batchSize, offset, ct);
            if (jobsResponse is null || jobsResponse.Jobs.Count == 0)
                break;

            var jobIds = jobsResponse.Jobs.Select(j => j.Id);
            var existingIds = await vectorStore.FilterExistingAsync(jobIds, ct);
            var newJobs = jobsResponse.Jobs.Where(j => !existingIds.Contains(j.Id)).ToList();

            if (newJobs.Count > 0)
            {
                var tasks = newJobs.Select(async job =>
                {
                    await semaphore.WaitAsync(ct);
                    try
                    {
                        var text = JobEmbeddingWorker.BuildJobText(job);
                        var embedding = await ollamaClient.GetEmbeddingAsync(text, ct);
                        if (embedding is null) return false;
                        await vectorStore.UpsertAsync(job.Id, embedding, ct);
                        return true;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var results = await Task.WhenAll(tasks);
                embedded += results.Count(r => r);
            }

            offset += batchSize;
            if (jobsResponse.Jobs.Count < batchSize)
                break;
        }

        if (embedded > 0)
        {
            _logger.LogInformation("Event-triggered embedding: embedded {Count} jobs into vector store", embedded);
        }
    }

    private static int GetRetryCount(IReadOnlyBasicProperties props)
    {
        if (props.Headers?.TryGetValue(RetryCountHeader, out var val) == true && val is long count)
            return (int)count;
        return 0;
    }

    private async Task InitializeRabbitMqAsync(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

            await _channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: _settings.Exchange,
                routingKey: RoutingKey,
                cancellationToken: ct);

            await _channel.BasicQosAsync(0, 5, false, ct);

            _logger.LogInformation(
                "Jobs-fetched consumer connected to RabbitMQ at {Host}:{Port}, queue: {Queue}",
                _settings.Host, _settings.Port, QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ for jobs-fetched consumer");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
            await _channel.CloseAsync(cancellationToken);
        if (_connection is not null)
            await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
