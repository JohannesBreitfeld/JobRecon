using System.Text;
using System.Text.Json;
using JobRecon.Contracts.Events;
using JobRecon.Infrastructure.Messaging;
using JobRecon.Matching.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JobRecon.Matching.Workers;

public sealed class JobsFetchedConsumer : BackgroundService
{
    private const string QueueName = "matching.jobs-fetched";
    private const string DeadLetterExchange = "jobrecon.dlx";
    private const string DeadLetterQueue = "dlq.matching.jobs-fetched";
    private const string DeadLetterRoutingKey = "dlq.matching.jobs-fetched";
    private const string RoutingKey = "jobs.fetched";

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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InitializeRabbitMqAsync(stoppingToken);

                if (_channel is null)
                {
                    _logger.LogWarning("RabbitMQ channel not initialized, retrying in 10s");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
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
                        _logger.LogError(ex, "Error processing jobs-fetched message, sending to DLQ");
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Jobs-fetched consumer loop failed, retrying in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task TriggerEmbeddingAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IJobEmbeddingService>();
        await embeddingService.EmbedPendingJobsAsync(ct);
    }

    private async Task InitializeRabbitMqAsync(CancellationToken ct)
    {
        (_connection, _channel) = await RabbitMqChannelFactory.CreateAsync(_settings, ct);

        // Declare dead-letter exchange and queue
        await _channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: ct);

        await _channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: DeadLetterRoutingKey,
            cancellationToken: ct);

        // Declare main queue with DLX routing
        var queueArgs = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = DeadLetterExchange,
            ["x-dead-letter-routing-key"] = DeadLetterRoutingKey
        };

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
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
