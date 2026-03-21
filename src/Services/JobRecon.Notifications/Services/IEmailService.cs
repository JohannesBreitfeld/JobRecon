using JobRecon.Notifications.Contracts;

namespace JobRecon.Notifications.Services;

public interface IEmailService
{
    Task<bool> SendJobMatchEmailAsync(
        string toEmail,
        string? toName,
        JobMatchEmailDto match,
        CancellationToken ct = default);

    Task<bool> SendDigestEmailAsync(
        string toEmail,
        string? toName,
        DigestEmailDto digest,
        CancellationToken ct = default);
}
