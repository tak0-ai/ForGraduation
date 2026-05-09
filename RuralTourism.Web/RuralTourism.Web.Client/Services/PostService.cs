using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using RuralTourism.Web.Client.Models;

namespace RuralTourism.Web.Client.Services;

public sealed class PostService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public PostService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> UploadMediaFromDataUrlAsync(string base64DataUrl)
    {
        // base64DataUrl like "data:image/png;base64,...."
        var comma = base64DataUrl.IndexOf(',');
        if (comma < 0) throw new ArgumentException("Invalid data URL");
        var meta = base64DataUrl.Substring(0, comma);
        var data = base64DataUrl.Substring(comma + 1);
        var content = new MultipartFormDataContent();
        var bytes = Convert.FromBase64String(data);
        var stream = new ByteArrayContent(bytes);
        var mime = "application/octet-stream";
        if (meta.StartsWith("data:"))
        {
            var semi = meta.IndexOf(';');
            mime = semi > 0 ? meta.Substring(5, semi - 5) : meta.Substring(5);
        }
        stream.Headers.ContentType = new MediaTypeHeaderValue(mime);
        content.Add(stream, "file", "upload.png");

        var response = await _http.PostAsync("api/media/upload", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"上传媒体失败: {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }
        var json = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
        if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("url", out var urlElement) && urlElement.ValueKind == JsonValueKind.String)
        {
            return urlElement.GetString() ?? string.Empty;
        }
        throw new InvalidOperationException($"上传媒体返回值不包含 url：{body}");
    }

    public async Task<List<PostSummaryDto>> GetPublishedPostsAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _http.GetFromJsonAsync<List<PostSummaryDto>>("api/posts", JsonOptions, cancellationToken);
        return posts ?? new List<PostSummaryDto>();
    }

    public async Task<List<PostSummaryDto>> GetRecommendedPostsAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        var posts = await _http.GetFromJsonAsync<List<PostSummaryDto>>($"api/recommendations/posts?take={take}", JsonOptions, cancellationToken);
        return posts ?? new List<PostSummaryDto>();
    }

    public async Task<List<PostSummaryDto>> SearchRecommendedPostsAsync(string keyword, int take = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return [];
        var encoded = Uri.EscapeDataString(keyword.Trim());
        var posts = await _http.GetFromJsonAsync<List<PostSummaryDto>>($"api/recommendations/search/posts?keyword={encoded}&take={take}", JsonOptions, cancellationToken);
        return posts ?? [];
    }

    public async Task<PostDetailDto?> GetPostAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<PostDetailDto>($"api/posts/{id}", JsonOptions, cancellationToken);
    }

    public async Task<bool> TrackPostInteractionAsync(string postId, string eventType = "View", string? metadata = null)
    {
        try
        {
            var url = $"api/posts/{postId}/track?eventType={Uri.EscapeDataString(eventType)}";
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                url += $"&metadata={Uri.EscapeDataString(metadata)}";
            }

            var response = await _http.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<PostSummaryDto>> GetMyPostsAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _http.GetFromJsonAsync<List<PostSummaryDto>>("api/posts/me", JsonOptions, cancellationToken);
        return posts ?? new List<PostSummaryDto>();
    }

    public async Task<List<PostSummaryDto>> GetPendingReviewPostsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _http.GetFromJsonAsync<List<PostSummaryDto>>($"api/posts/pending-reviews?page={page}&pageSize={pageSize}", JsonOptions, cancellationToken);
        return posts ?? [];
    }

    public async Task<List<PostSummaryDto>> GetMyCollectionsAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _http.GetFromJsonAsync<List<PostSummaryDto>>("api/posts/collections", JsonOptions, cancellationToken);
        return posts ?? new List<PostSummaryDto>();
    }

    public async Task<string> UploadMediaAsync(IBrowserFile file)
    {
        var result = await UploadMediaWithIdAsync(file);
        return result.Url;
    }

    public async Task<(string Id, string Url)> UploadMediaWithIdAsync(IBrowserFile file)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024));
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(streamContent, "file", file.Name);

        var response = await _http.PostAsync("api/media/upload", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"上传媒体失败: {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }

        var json = JsonSerializer.Deserialize<JsonElement>(body, JsonOptions);
        if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("id", out var idElement) && json.TryGetProperty("url", out var urlElement))
        {
            return (idElement.GetString() ?? string.Empty, urlElement.GetString() ?? string.Empty);
        }

        throw new InvalidOperationException($"上传媒体返回值不包含 id/url：{body}");
    }

    public async Task<PostDetailDto> CreatePostAsync(PostCreateDto dto, IBrowserFile? coverImage = null)
    {
        using var content = CreateMultipartContent(dto, coverImage);
        var response = await _http.PostAsync("api/posts", content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PostDetailDto>(JsonOptions)
            ?? throw new InvalidOperationException("未能从服务端获取新文章信息。");
    }

    public async Task UpdatePostAsync(string id, PostCreateDto dto, IBrowserFile? coverImage = null)
    {
        using var content = CreateMultipartContent(dto, coverImage);
        var response = await _http.PutAsync($"api/posts/{id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeletePostAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/posts/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task SetPostStatusAsync(string id, PostStatus status)
    {
        var response = await _http.PatchAsync($"api/posts/{id}/status?status={status}", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReviewPostAsync(string id, bool approve, string? reason = null)
    {
        var payload = new
        {
            approve,
            reason
        };

        var response = await _http.PostAsJsonAsync($"api/posts/{id}/review", payload, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<CommentDto>> GetCommentsAsync(string postId)
    {
        return await _http.GetFromJsonAsync<List<CommentDto>>($"api/comments/post/{postId}", JsonOptions) ?? [];
    }

    public async Task<CommentDto> CreateCommentAsync(string postId, CommentCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/comments/post/{postId}", dto, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommentDto>(JsonOptions) ?? throw new InvalidOperationException();
    }

    public async Task DeleteCommentAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/comments/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> TogglePostReactionAsync(string postId, int type)
    {
        // 0=Like, 1=Bookmark (See ReactionType enum)
        var response = await _http.PostAsync($"api/reactions/post/{postId}/{type}", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactionResult>(JsonOptions);
        return result?.IsAdded ?? false;
    }

    public async Task<bool> ToggleCommentReactionAsync(string commentId, int type)
    {
        var response = await _http.PostAsync($"api/reactions/comment/{commentId}/{type}", null);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReactionResult>(JsonOptions);
        return result?.IsAdded ?? false;
    }

    private class ReactionResult
    {
        public bool IsAdded { get; set; }
    }


    private MultipartFormDataContent CreateMultipartContent(PostCreateDto dto, IBrowserFile? coverImage)

    {
        var content = new MultipartFormDataContent();
        var serialized = JsonSerializer.Serialize(dto, JsonOptions);
        // Use plain text part so model binding for [FromForm] string postJson works consistently
        content.Add(new StringContent(serialized, Encoding.UTF8, "text/plain"), "postJson");

        if (coverImage != null)
        {
            var streamContent = new StreamContent(coverImage.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024));
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(coverImage.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "coverImage", coverImage.Name);
        }

        return content;
    }
}
