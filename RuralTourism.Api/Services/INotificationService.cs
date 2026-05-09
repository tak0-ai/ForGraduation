using RuralTourism.Api.Entities;

namespace RuralTourism.Api.Services;

public interface INotificationService
{
    Task AddNotificationAsync(string userId, string title, string? body, string? postId = null, string? triggerUserId = null, CancellationToken cancellationToken = default);
}
