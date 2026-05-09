using RuralTourism.Web.Client.Models;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace RuralTourism.Web.Client.Services;

public class ChatService : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly TokenStorage _tokenStorage;
    private readonly string _hubUrl;
    private HubConnection? _hubConnection;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public event Action<ChatMessageDto>? OnMessageReceived;

    public ChatService(HttpClient http, TokenStorage tokenStorage, Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _http = http;
        _tokenStorage = tokenStorage;
        var apiBaseUrl = config["BackendApi:BaseUrl"];
        // Ensure valid hub url
        _hubUrl = string.IsNullOrWhiteSpace(apiBaseUrl) 
            ? "/hubs/chat" // fallback relative
            : $"{apiBaseUrl.TrimEnd('/')}/hubs/chat"; // absolute
    }

    public async Task InitializeAsync()
    {
        if (_hubConnection != null) return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl((_http.BaseAddress != null ? new Uri(_http.BaseAddress, "/hubs/chat") : new Uri(_hubUrl)), options =>
            {
                options.AccessTokenProvider = async () => await _tokenStorage.GetTokenAsync();
            }) // Simpler config
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<ChatMessageDto>("ReceiveMessage", (msg) =>
        {
            OnMessageReceived?.Invoke(msg);
        });

        await _hubConnection.StartAsync();
    }

    public async Task JoinRoomAsync(string roomId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinChatRoom", roomId);
        }
    }
    
    public async Task LeaveRoomAsync(string roomId)
    {
         if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveChatRoom", roomId);
        }
    }

    public async Task<List<ChatRoomDto>> GetMyRoomsAsync()
    {
        return await _http.GetFromJsonAsync<List<ChatRoomDto>>("api/chat/rooms", JsonOptions) ?? [];
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(string roomId)
    {
        return await _http.GetFromJsonAsync<List<ChatMessageDto>>($"api/chat/rooms/{roomId}/messages", JsonOptions) ?? [];
    }

    public async Task<ChatMessageDto> SendMessageAsync(ChatMessageCreateDto dto)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/messages", dto, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<ChatMessageDto>(JsonOptions) ?? throw new InvalidOperationException();
    }

    public async Task<string> Create1on1ChatAsync(string targetUserId)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/rooms", new ChatRoomCreateDto { IsGroup = false, TargetUserId = targetUserId }, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }
    
    public async Task<ChatRoomDto?> CreateGroupChatAsync(string name, List<string> memberIds)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/rooms", new ChatRoomCreateDto 
        { 
            IsGroup = true, 
            Name = name,
            MemberIds = memberIds 
        }, JsonOptions);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<ChatRoomDto>(JsonOptions);
        return data; 
    }

    public async Task<List<SearchResultDto>> SearchAsync(string query, string type = "All")
    {
        return await _http.GetFromJsonAsync<List<SearchResultDto>>($"api/chat/search?query={Uri.EscapeDataString(query)}&type={type}", JsonOptions) ?? [];
    }

    public async Task JoinGroupAsync(string groupId)
    {
        var resp = await _http.PostAsync($"api/chat/groups/{groupId}/join", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task SendFriendRequestAsync(string targetUserId)
    {
        var resp = await _http.PostAsync($"api/chat/requests/friend/{targetUserId}", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<ChatRequestDto>> GetRequestsAsync()
    {
        return await _http.GetFromJsonAsync<List<ChatRequestDto>>("api/chat/requests", JsonOptions) ?? [];
    }

    public async Task HandleRequestAsync(string requestId, bool accept)
    {
        string action = accept ? "accept" : "reject";
        var resp = await _http.PostAsync($"api/chat/requests/{requestId}/handle/{action}", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<UserInfoDto>> GetFriendsAsync()
    {
        return await _http.GetFromJsonAsync<List<UserInfoDto>>("api/chat/friends", JsonOptions) ?? [];
    }

    public async Task DeleteFriendAsync(string targetUserId)
    {
        var resp = await _http.DeleteAsync($"api/chat/friends/{targetUserId}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task MuteMemberAsync(string roomId, string targetUserId, int minutes)
    {
        var resp = await _http.PostAsJsonAsync($"api/chat/rooms/{roomId}/mute", new { TargetUserId = targetUserId, Minutes = minutes });
        resp.EnsureSuccessStatusCode();
    }

    public async Task KickMemberAsync(string roomId, string targetUserId)
    {
        var resp = await _http.PostAsJsonAsync($"api/chat/rooms/{roomId}/kick", targetUserId);
        resp.EnsureSuccessStatusCode();
    }

    public async Task LeaveGroupAsync(string groupId)
    {
        var resp = await _http.PostAsync($"api/chat/groups/{groupId}/leave", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DismissGroupAsync(string groupId)
    {
        var resp = await _http.PostAsync($"api/chat/groups/{groupId}/dismiss", null);
        resp.EnsureSuccessStatusCode();
    }

    public async Task RenameGroupAsync(string groupId, string newName)
    {
        var resp = await _http.PostAsJsonAsync($"api/chat/groups/{groupId}/rename", newName);
        resp.EnsureSuccessStatusCode();
    }

    public async Task InviteToGroupAsync(string groupId, string targetUserId)
    {
        var resp = await _http.PostAsJsonAsync($"api/chat/groups/{groupId}/invite", targetUserId);
        resp.EnsureSuccessStatusCode();
    }

    public async Task SetGroupRoleAsync(string groupId, string targetUserId, bool isAdmin)
    {
        var resp = await _http.PostAsJsonAsync($"api/chat/groups/{groupId}/role", new { TargetUserId = targetUserId, IsAdmin = isAdmin });
        resp.EnsureSuccessStatusCode();
    }




    public async ValueTask DisposeAsync()
    {
         if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
