using System.Net.Http.Json;
using RuralTourism.Web.Client.Models;

namespace RuralTourism.Web.Client.Services;

public sealed class UserService
{
    private readonly HttpClient _http;

    public UserService(HttpClient http)
    {
        _http = http;
    }

    public async Task<UserInfoDto?> GetUserInfoAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<UserInfoDto>("api/users/me");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateUserInfoAsync(UpdateUserDto dto)
    {
        var response = await _http.PutAsJsonAsync("api/users/me", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<UserInfoDto>> GetFriendsAsync()
    {
        try
        {
             return await _http.GetFromJsonAsync<List<UserInfoDto>>("api/users/me/following") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<AdminUserDto>> GetAdminUsersAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<AdminUserDto>>("api/users/admin") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> BanUserAsync(string userId, DateTimeOffset banUntil, bool isPermanent = false)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/users/{userId}/ban", new BanUserRequestDto { BanUntil = banUntil, IsPermanent = isPermanent });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, string role)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/users/{userId}/role", new UpdateUserRoleDto { Role = role });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnbanUserAsync(string userId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/users/{userId}/ban");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PublicUserProfileDto?> GetPublicProfileAsync(string userNo)
    {
        try
        {
            return await _http.GetFromJsonAsync<PublicUserProfileDto>($"api/users/public/{userNo}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PostSummaryDto>> GetPublicPostsAsync(string userNo, int page = 1, int pageSize = 12)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<PostSummaryDto>>($"api/users/public/{userNo}/posts?page={page}&pageSize={pageSize}") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool?> ToggleFollowAsync(string userNo)
    {
        try
        {
            var response = await _http.PostAsync($"api/users/public/{userNo}/follow", null);
            if (!response.IsSuccessStatusCode) return null;
            var result = await response.Content.ReadFromJsonAsync<FollowResultDto>();
            return result?.IsFollowing;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<UserWallMessageDto>> GetPublicWallMessagesAsync(string userNo)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserWallMessageDto>>($"api/users/public/{userNo}/messages") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<UserWallMessageDto?> CreatePublicWallMessageAsync(string userNo, string content)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"api/users/public/{userNo}/messages", new UserWallMessageCreateDto { Content = content });
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserWallMessageDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<UserSimpleDto>> GetPublicFollowingAsync(string userNo)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserSimpleDto>>($"api/users/public/{userNo}/following") ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<UserSimpleDto>> GetPublicFollowersAsync(string userNo)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<UserSimpleDto>>($"api/users/public/{userNo}/followers") ?? [];
        }
        catch
        {
            return [];
        }
    }

    private sealed class FollowResultDto
    {
        public bool IsFollowing { get; set; }
    }
}