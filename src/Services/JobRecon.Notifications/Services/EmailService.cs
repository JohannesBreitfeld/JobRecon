using System.Reflection;
using JobRecon.Notifications.Configuration;
using JobRecon.Notifications.Contracts;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace JobRecon.Notifications.Services;

public class EmailService : IEmailService
{
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
        CancellationToken ct = default)
    {
        var subject = $"New Job Match: {match.JobTitle} at {match.CompanyName}";
        var template = await LoadTemplateAsync("JobMatchEmail.html");

        var body = template
            .Replace("{{JobTitle}}", match.JobTitle)
            .Replace("{{CompanyName}}", match.CompanyName)
            .Replace("{{Location}}", match.Location ?? "Not specified")
            .Replace("{{MatchScore}}", $"{match.MatchScore:P0}")
            .Replace("{{TopFactors}}", FormatMatchFactors(match.TopFactors))
            .Replace("{{JobUrl}}", match.JobUrl ?? "#");

        return await SendEmailAsync(toEmail, toName, subject, body, ct);
    }

    public async Task<bool> SendDigestEmailAsync(
        string toEmail,
        string? toName,
        DigestEmailDto digest,
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

        return await SendEmailAsync(toEmail, toName, subject, body, ct);
    }

    private async Task<bool> SendEmailAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, _settings.UseSsl, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully to {ToEmail}: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}: {Subject}", toEmail, subject);
            return false;
        }
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
            $"<li><strong>{f.Category}</strong>: {f.Score:P0} - {f.Description ?? "Good match"}</li>");

        return $"<ul>{string.Join("\n", items)}</ul>";
    }

    private static string FormatDigestJob(DigestItemDto job)
    {
        return $"""
            <div style="border:1px solid #ddd;padding:10px;margin:10px 0;">
                <h3><a href="{job.JobUrl ?? "#"}">{job.JobTitle}</a></h3>
                <p><strong>{job.CompanyName}</strong> - {job.Location ?? "Location not specified"}</p>
                <p>Match Score: {job.MatchScore:P0}</p>
            </div>
            """;
    }
}
