using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System.Security.Claims;

namespace RuralTourism.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public NotificationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var query = _db.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt);

                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new
                    {
                        n.Id,
                        n.Title,
                        n.Body,
                        n.Level,
                        n.IsRead,
                        n.CreatedAt,
                        n.PostId,
                        n.TourPlanId,
                        n.ChatRoomId,
                        n.TriggerUserId,
                        TriggerUserNo = n.TriggerUser != null ? n.TriggerUser.UserNo.ToString("D6") : null,
                        TriggerUserName = n.TriggerUser != null ? (n.TriggerUser.Nickname ?? n.TriggerUser.UserName) : null,
                        TriggerUserAvatarUrl = n.TriggerUser != null ? n.TriggerUser.AvatarUrl : null
                    })
                    .ToListAsync();

                var totalUnread = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

                return Ok(new { items, totalUnread });
            }
            catch (Exception ex)
            {
                // Return detailed error for debugging
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace, innerException = ex.InnerException?.Message });
            }
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notification = await _db.Notifications.FindAsync(id);
            if (notification == null) return NotFound();
            
            if (notification.UserId != userId) return Forbid();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var unread = await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var n in unread) n.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var count = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Ok(count);
        }
    }
}