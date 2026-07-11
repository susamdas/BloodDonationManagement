using System.Net;
using System.Net.Mail;

namespace BloodDonationManagement.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPass { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendBloodRequestAlertAsync(string to, string bloodGroup, string patientName, string district, string urgency, string contactNo);
    Task SendDonorEligibilityReminderAsync(string to, string donorName);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _settings = config.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpPass))
        {
            _logger.LogWarning("Email not sent to {To} — SmtpPass is not configured in appsettings.", to);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPass),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }

    public async Task SendBloodRequestAlertAsync(string to, string bloodGroup, string patientName, string district, string urgency, string contactNo)
    {
        var urgencyColor = urgency == "Critical" ? "#dc3545" : urgency == "Urgent" ? "#fd7e14" : "#198754";
        var subject = $"[{urgency}] Blood Request: {bloodGroup} needed in {district}";
        var body = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;border:1px solid #eee;border-radius:8px;overflow:hidden">
              <div style="background:{urgencyColor};color:white;padding:20px 24px">
                <h2 style="margin:0">🩸 {urgency} Blood Request</h2>
              </div>
              <div style="padding:24px">
                <table style="width:100%;border-collapse:collapse">
                  <tr><td style="padding:8px 0;color:#666">Blood Group</td><td style="font-weight:bold;color:#dc3545">{bloodGroup}</td></tr>
                  <tr><td style="padding:8px 0;color:#666">Patient</td><td style="font-weight:bold">{patientName}</td></tr>
                  <tr><td style="padding:8px 0;color:#666">District</td><td>{district}</td></tr>
                  <tr><td style="padding:8px 0;color:#666">Contact</td><td><a href="tel:{contactNo}">{contactNo}</a></td></tr>
                </table>
                <p style="margin-top:20px;color:#555">
                  If you can donate, please call the contact number above immediately.
                </p>
              </div>
              <div style="background:#f8f9fa;padding:12px 24px;color:#999;font-size:12px">
                BloodConnect BD — Saving lives across Bangladesh
              </div>
            </div>
            """;

        await SendAsync(to, subject, body);
    }

    public async Task SendDonorEligibilityReminderAsync(string to, string donorName)
    {
        var subject = "You are now eligible to donate blood again!";
        var body = $"""
            <div style="font-family:sans-serif;max-width:560px;margin:auto;border:1px solid #eee;border-radius:8px;overflow:hidden">
              <div style="background:#dc3545;color:white;padding:20px 24px">
                <h2 style="margin:0">🩸 You Can Donate Again!</h2>
              </div>
              <div style="padding:24px">
                <p>Dear <strong>{donorName}</strong>,</p>
                <p>It has been 90 days since your last donation. You are now eligible to donate blood again!</p>
                <p>There may be urgent blood requests in your area waiting for a donor like you.</p>
                <a href="#" style="display:inline-block;background:#dc3545;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold">
                  View Blood Requests
                </a>
              </div>
              <div style="background:#f8f9fa;padding:12px 24px;color:#999;font-size:12px">
                BloodConnect BD — Saving lives across Bangladesh
              </div>
            </div>
            """;

        await SendAsync(to, subject, body);
    }
}
