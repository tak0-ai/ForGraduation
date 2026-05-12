using System.Net.Http.Json;
using System.Text.Json;
using RuralTourism.Web.Client.Models;

namespace RuralTourism.Web.Client.Services;

public class NotificationService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public NotificationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<NotificationListResult> GetMyNotificationsAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<NotificationListResult>($"api/notifications?page={page}&pageSize={pageSize}", JsonOptions) 
               ?? new NotificationListResult();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<int>("api/notifications/unread-count");
        }
        catch
        {
            return 0;
        }
    }

    public async Task MarkAsReadAsync(string id)
    {
        await _http.PatchAsync($"api/notifications/{id}/read", null);
    }

    public async Task MarkAllAsReadAsync()
    {
        await _http.PatchAsync("api/notifications/read-all", null);
    }
}

public class NotificationListResult
{
    public List<NotificationDto> Items { get; set; } = [];
    public int TotalUnread { get; set; }
}
