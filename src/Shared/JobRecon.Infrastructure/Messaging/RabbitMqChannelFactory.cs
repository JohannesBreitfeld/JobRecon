using RabbitMQ.Client;

namespace JobRecon.Infrastructure.Messaging;

public static class RabbitMqChannelFactory
{
    public static async Task<(IConnection Connection, IChannel Channel)> CreateAsync(
        RabbitMqSettings settings,
        CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.Username,
            Password = settings.Password,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        var connection = await factory.CreateConnectionAsync(ct);
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: settings.Exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        return (connection, channel);
    }
}
