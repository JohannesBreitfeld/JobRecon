namespace JobRecon.Notifications.Configuration;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "jobrecon.events";
    public string Queue { get; set; } = "notifications";
    public string RoutingKey { get; set; } = "match.created";
}
