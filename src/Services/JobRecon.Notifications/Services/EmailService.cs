using System.Net;
using System.Reflection;
using JobRecon.Notifications.Configuration;
using JobRecon.Notifications.Contracts;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace JobRecon.Notifications.Services;

public sealed class EmailService : IEmailService
{
    private const int MaxRetries = 3;

    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendJobMatchEmailAsync(
        string toEmail,
        string? toName,
        JobMatchEmailDto match,
        string? unsubscribeToken = null,
        CancellationToken ct = default)
    {
        var subject = $"New Job Match: {match.JobTitle} at {match.CompanyName}";
        var template = await LoadTemplateAsync("JobMatchEmail.html");

        var body = template
            .Replace("{{JobTitle}}", WebUtility.HtmlEncode(match.JobTitle))
            .Replace("{{CompanyName}}", WebUtility.HtmlEncode(match.CompanyName))
            .Replace("{{Location}}", WebUtility.HtmlEncode(match.Location ?? "Not specified"))
            .Replace("{{MatchScore}}", $"{match.MatchScore:P0}")
            .Replace("{{TopFactors}}", FormatMatchFactors(match.TopFactors))
            .Replace("{{JobUrl}}", SanitizeUrl(match.JobUrl));

        body = AppendUnsubscribeFooter(body, unsubscribeToken);

        return await SendEmailWithRetryAsync(toEmail, toName, subject, body, ct);
    }

    public async Task<bool> SendDigestEmailAsync(
        string toEmail,
        string? toName,
        DigestEmailDto digest,
        string? unsubscribeToken = null,
        CancellationToken ct = default)
    {
        var subject = $"Your Daily Job Matches - {digest.TotalJobCount} new opportunities";
        var template = await LoadTemplateAsync("DigestEmail.html");

        var jobsHtml = string.Join("\n", digest.Jobs.Select(FormatDigestJob));

        var body = template
            .Replace("{{TotalJobCount}}", digest.TotalJobCount.ToString())
            .Replace("{{PeriodStart}}", digest.PeriodStart.ToString("MMM dd"))
            .Replace("{{PeriodEnd}}", digest.PeriodEnd.ToString("MMM dd, yyyy"))
            .Replace("{{Jobs}}", jobsHtml);

        body = AppendUnsubscribeFooter(body, unsubscribeToken);

        return await SendEmailWithRetryAsync(toEmail, toName, subject, body, ct);
    }

    private async Task<bool> SendEmailWithRetryAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
                message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl, ct);

                if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
                {
                    await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
                }

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                _logger.LogInformation("Email sent successfully to {ToEmail}: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex,
                    "Failed to send email to {ToEmail} (attempt {Attempt}/{Max}), retrying in {Delay}",
                    toEmail, attempt, MaxRetries, delay);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email to {ToEmail}: {Subject} after {Max} attempts",
                    toEmail, subject, MaxRetries);
            }
        }

        return false;
    }

    private string AppendUnsubscribeFooter(string htmlBody, string? unsubscribeToken)
    {
        var footer = unsubscribeToken is not null
            ? $"""
                <hr style="margin-top:30px;border:none;border-top:1px solid #ddd;">
                <p style="font-size:12px;color:#888;text-align:center;">
                    You are receiving this because you have email notifications enabled on JobRecon.<br/>
                    <a href="{_settings.BaseUrl}/api/notifications/unsubscribe?token={unsubscribeToken}">Unsubscribe from emails</a>
                    &nbsp;|&nbsp;
                    <a href="{_settings.BaseUrl}/settings/notifications">Manage preferences</a>
                </p>
                """
            : """
                <hr style="margin-top:30px;border:none;border-top:1px solid #ddd;">
                <p style="font-size:12px;color:#888;text-align:center;">
                    You are receiving this because you have email notifications enabled on JobRecon.<br/>
                    <a href="#">Manage preferences</a>
                </p>
                """;

        // Insert before closing </body> if present, otherwise append
        if (htmlBody.Contains("</body>", StringComparison.OrdinalIgnoreCase))
        {
            return htmlBody.Replace("</body>", footer + "\n</body>", StringComparison.OrdinalIgnoreCase);
        }

        return htmlBody + footer;
    }

    private static async Task<string> LoadTemplateAsync(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"JobRecon.Notifications.Templates.{templateName}";

        await using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            return GetDefaultTemplate(templateName);
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private static string GetDefaultTemplate(string templateName)
    {
        return templateName switch
        {
            "JobMatchEmail.html" => """
                <!DOCTYPE html>
                <html>
                <head><style>body{font-family:Arial,sans-serif;}</style></head>
                <body>
                    <h1>New Job Match Found!</h1>
                    <h2>{{JobTitle}}</h2>
                    <p><strong>Company:</strong> {{CompanyName}}</p>
                    <p><strong>Location:</strong> {{Location}}</p>
                    <p><strong>Match Score:</strong> {{MatchScore}}</p>
                    <h3>Why this job matches:</h3>
                    {{TopFactors}}
                    <p><a href="{{JobUrl}}">View Job Posting</a></p>
                </body>
                </html>
                """,
            "DigestEmail.html" => """
                <!DOCTYPE html>
                <html>
                <head><style>body{font-family:Arial,sans-serif;}</style></head>
                <body>
                    <h1>Your Job Digest</h1>
                    <p>{{TotalJobCount}} new job matches from {{PeriodStart}} to {{PeriodEnd}}</p>
                    {{Jobs}}
                </body>
                </html>
                """,
            _ => "<html><body><p>Template not found</p></body></html>"
        };
    }

    private static string FormatMatchFactors(List<MatchFactorData> factors)
    {
        if (factors.Count == 0)
        {
            return "<p>No specific match factors available.</p>";
        }

        var items = factors.Select(f =>
            $"<li><strong>{WebUtility.HtmlEncode(f.Category)}</strong>: {f.Score:P0} - {WebUtility.HtmlEncode(f.Description ?? "Good match")}</li>");

        return $"<ul>{string.Join("\n", items)}</ul>";
    }

    private static string FormatDigestJob(DigestItemDto job)
    {
        return $"""
            <div style="border:1px solid #ddd;padding:10px;margin:10px 0;">
                <h3><a href="{SanitizeUrl(job.JobUrl)}">{WebUtility.HtmlEncode(job.JobTitle)}</a></h3>
                <p><strong>{WebUtility.HtmlEncode(job.CompanyName)}</strong> - {WebUtility.HtmlEncode(job.Location ?? "Location not specified")}</p>
                <p>Match Score: {job.MatchScore:P0}</p>
            </div>
            """;
    }

    private static string SanitizeUrl(string? url)
    {
        if (url is null)
            return "#";

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? WebUtility.HtmlEncode(url)
            : "#";
    }
}
