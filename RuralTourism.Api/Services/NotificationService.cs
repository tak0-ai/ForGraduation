using RuralTourism.Api.Entities;
using RuralTourism.Api.Enums;
using RuralTourism.Api.Migrations;

namespace RuralTourism.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddNotificationAsync(string userId, string title, string? body, string? postId = null, string? triggerUserId = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Level = NotificationLevel.Info,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            PostId = postId,
            TriggerUserId = triggerUserId
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
