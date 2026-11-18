using BloodDonationManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationManagement.Controllers;

/// <summary>In-app notification bell (FR-4.1).</summary>
[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string UserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var items = await _context.Notifications
            .Where(n => n.UserId == UserId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(15)
            .Select(n => new
            {
                id = n.NotificationId,
                n.Title,
                n.Message,
                n.Link,
                n.Type,
                n.IsRead,
                n.CreatedAt
            })
            .ToListAsync();

        var unread = await _context.Notifications.CountAsync(n => n.UserId == UserId && !n.IsRead);

        return Ok(new
        {
            unread,
            items = items.Select(n => new
            {
                n.id, n.Title, n.Message, n.Link, n.Type, n.IsRead,
                createdAt = n.CreatedAt.ToString("dd MMM, hh:mm tt")
            })
        });
    }

    [HttpPost("read-all")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll()
    {
        await _context.Notifications
            .Where(n => n.UserId == UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }
}
