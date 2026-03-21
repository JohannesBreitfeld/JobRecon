using System.Text;
using System.Text.Json;
using JobRecon.Matching.Configuration;
using JobRecon.Matching.Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace JobRecon.Matching.Services;

public sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isInitialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqEventPublisher(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishJobMatchedAsync(JobMatchedEvent eventData, CancellationToken ct = default)
    {
        try
        {
            await EnsureInitializedAsync(ct);

            if (_channel is null)
            {
                _logger.LogWarning("RabbitMQ channel not available, skipping event publish");
                return;
            }

            var message = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = eventData.EventId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _settings.Exchange,
                routingKey: _settings.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: ct);

            _logger.LogDebug(
                "Published JobMatchedEvent {EventId} for user {UserId}, job {JobId}",
                eventData.EventId, eventData.UserId, eventData.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish JobMatchedEvent {EventId}", eventData.EventId);
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_isInitialized) return;

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

            _isInitialized = true;

            _logger.LogInformation(
                "Connected to RabbitMQ at {Host}:{Port}",
                _settings.Host, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _initLock.Dispose();
    }
}
