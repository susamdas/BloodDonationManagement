using BloodDonationManagement.Data;
using BloodDonationManagement.Hubs;
using BloodDonationManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace BloodDonationManagement.Services;

/// <summary>
/// Sends a notification to a user through every channel we have:
/// a stored in-app notification (bell), a live SignalR push and (optionally) an email.
/// </summary>
public class NotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly IEmailService _email;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IHubContext<NotificationHub> hub,
        IEmailService email,
        UserManager<IdentityUser> userManager,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _hub = hub;
        _email = email;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>Stores the notification and pushes it live. Call SaveChanges is done here.</summary>
    public async Task NotifyAsync(string? userId, string title, string message, string? link = null,
                                  string type = "info", string? emailSubject = null, string? emailHtml = null)
    {
        if (string.IsNullOrEmpty(userId)) return;

        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Link = link,
            Type = type
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        try
        {
            await _hub.Clients.User(userId).SendAsync("Notify", new
            {
                id = notification.NotificationId,
                title,
                message,
                link,
                type,
                createdAt = notification.CreatedAt.ToString("dd MMM, hh:mm tt")
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR push failed for user {UserId}", userId);
        }

        if (emailSubject != null && emailHtml != null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (!string.IsNullOrEmpty(user?.Email))
                await _email.SendAsync(user.Email, emailSubject, emailHtml);
        }
    }
}
