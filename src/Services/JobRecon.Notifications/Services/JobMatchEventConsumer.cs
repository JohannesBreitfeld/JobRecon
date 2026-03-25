using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using JobRecon.Notifications.Configuration;
using JobRecon.Contracts.Events;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Domain;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JobRecon.Notifications.Services;

public sealed class JobMatchEventConsumer : BackgroundService, IJobMatchEventConsumer
{
    private const string RetryCountHeader = "x-retry-count";
    private const int MaxRetries = 3;

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobMatchEventConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public JobMatchEventConsumer(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<JobMatchEventConsumer> logger)
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
            _logger.LogWarning("RabbitMQ channel not initialized, event consumer will not run");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var jobMatchedEvent = JsonSerializer.Deserialize<JobMatchedEvent>(message);

                if (jobMatchedEvent is not null)
                {
                    await ProcessEventAsync(jobMatchedEvent, stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                var retryCount = GetRetryCount(ea.BasicProperties);
                if (retryCount < MaxRetries)
                {
                    _logger.LogWarning(ex,
                        "Error processing job-matched message, retry {Attempt}/{Max}",
                        retryCount + 1, MaxRetries);

                    var props = new BasicProperties { Persistent = true };
                    props.Headers = new Dictionary<string, object?> { [RetryCountHeader] = (long)(retryCount + 1) };
                    await _channel.BasicPublishAsync(
                        _settings.Exchange, _settings.RoutingKey, true, props, ea.Body, stoppingToken);
                }
                else
                {
                    _logger.LogError(ex,
                        "job-matched message exceeded max retries ({Max}), discarding", MaxRetries);
                }

                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: _settings.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Started consuming messages from queue {Queue}", _settings.Queue);

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
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

            // Declare exchange
            await _channel.ExchangeDeclareAsync(
                exchange: _settings.Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: ct);

            // Declare queue
            await _channel.QueueDeclareAsync(
                queue: _settings.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct);

            // Bind queue to exchange
            await _channel.QueueBindAsync(
                queue: _settings.Queue,
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
                cancellationToken: ct);

            // Set prefetch count
            await _channel.BasicQosAsync(0, 10, false, ct);

            _logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}, queue: {Queue}",
                _settings.Host, _settings.Port, _settings.Queue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
        }
    }

    private async Task ProcessEventAsync(JobMatchedEvent eventData, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var preferenceService = scope.ServiceProvider.GetRequiredService<IPreferenceService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var digestService = scope.ServiceProvider.GetRequiredService<IDigestService>();
        var profileClient = scope.ServiceProvider.GetRequiredService<IProfileClient>();

        // Check for duplicate events
        if (await notificationService.HasEventBeenProcessedAsync(eventData.EventId, ct))
        {
            _logger.LogDebug("Event {EventId} already processed, skipping", eventData.EventId);
            return;
        }

        // Get user preferences
        var preferences = await preferenceService.GetOrCreatePreferencesAsync(eventData.UserId, ct);

        // Check if match score meets realtime threshold
        var isRealtimeAlert = eventData.MatchScore >= preferences.MinMatchScoreForRealtime;

        if (isRealtimeAlert)
        {
            await ProcessRealtimeAlertAsync(
                eventData, preferences, notificationService, emailService, profileClient, ct);
        }
        else if (preferences.DigestEnabled)
        {
            // Queue for digest
            var topFactorsJson = JsonSerializer.Serialize(eventData.TopFactors);
            await digestService.QueueForDigestAsync(
                eventData.UserId,
                eventData.JobId,
                eventData.JobTitle,
                eventData.CompanyName,
                eventData.MatchScore,
                eventData.Location,
                topFactorsJson,
                eventData.JobUrl,
                ct);
        }

        _logger.LogInformation(
            "Processed event {EventId} for user {UserId}, realtime: {IsRealtime}",
            eventData.EventId, eventData.UserId, isRealtimeAlert);
    }

    private async Task ProcessRealtimeAlertAsync(
        JobMatchedEvent eventData,
        NotificationPreference preferences,
        INotificationService notificationService,
        IEmailService emailService,
        IProfileClient profileClient,
        CancellationToken ct)
    {
        var jobMatchData = new JobMatchData(
            eventData.JobId,
            eventData.JobTitle,
            eventData.CompanyName,
            eventData.Location,
            eventData.MatchScore,
            eventData.TopFactors.Select(f => new MatchFactorData(f.Category, f.Score, f.Description)).ToList(),
            eventData.JobUrl);

        var dataJson = JsonSerializer.Serialize(jobMatchData);

        // Create in-app notification
        if (preferences.InAppEnabled)
        {
            await notificationService.CreateNotificationAsync(
                eventData.UserId,
                NotificationType.NewMatch,
                NotificationChannel.InApp,
                $"New Match: {eventData.JobTitle}",
                $"{eventData.MatchScore:P0} match at {eventData.CompanyName}",
                dataJson,
                eventData.EventId,
                ct);
        }

        // Send email notification
        if (preferences.EmailEnabled)
        {
            var email = preferences.OverrideEmail;

            if (string.IsNullOrEmpty(email))
            {
                var userEmail = await profileClient.GetUserEmailAsync(eventData.UserId, ct);
                email = userEmail?.Email;
            }

            if (!string.IsNullOrEmpty(email))
            {
                var emailDto = new JobMatchEmailDto(
                    eventData.JobId,
                    eventData.JobTitle,
                    eventData.CompanyName,
                    eventData.Location,
                    eventData.MatchScore,
                    eventData.TopFactors.Select(f => new MatchFactorData(f.Category, f.Score, f.Description)).ToList(),
                    eventData.JobUrl);

                await emailService.SendJobMatchEmailAsync(email, null, emailDto, preferences.UnsubscribeToken, ct);

                // Create email notification record
                await notificationService.CreateNotificationAsync(
                    eventData.UserId,
                    NotificationType.NewMatch,
                    NotificationChannel.Email,
                    $"New Match: {eventData.JobTitle}",
                    $"{eventData.MatchScore:P0} match at {eventData.CompanyName}",
                    dataJson,
                    ct: ct);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer");

        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
