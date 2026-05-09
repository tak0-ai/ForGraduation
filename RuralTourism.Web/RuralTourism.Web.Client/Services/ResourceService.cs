using System.Net.Http.Json;
using System.Text.Json;
using RuralTourism.Web.Client.Models;

namespace RuralTourism.Web.Client.Services;

public class ResourceService
{
    // 统一封装资源相关 API 调用
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ResourceService(HttpClient http)
    {
        _http = http;
    }

    // ===== 资源查询 =====
    public async Task<List<AttractionDto>> GetAttractionsAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<List<AttractionDto>>($"api/resources/attractions?page={page}&pageSize={pageSize}", JsonOptions) ?? [];
    }

    public async Task<AttractionDto?> GetAttractionAsync(string id)
    {
        return await _http.GetFromJsonAsync<AttractionDto>($"api/resources/attractions/{id}", JsonOptions);
    }

    public async Task<List<AccommodationDto>> GetAccommodationsAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<List<AccommodationDto>>($"api/resources/accommodations?page={page}&pageSize={pageSize}", JsonOptions) ?? [];
    }
    
    public async Task<AccommodationDto?> GetAccommodationAsync(string id)
    {
        return await _http.GetFromJsonAsync<AccommodationDto>($"api/resources/accommodations/{id}", JsonOptions);
    }

    public async Task<List<DiningDto>> GetDiningAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<List<DiningDto>>($"api/resources/dining?page={page}&pageSize={pageSize}", JsonOptions) ?? [];
    }
    
    public async Task<DiningDto?> GetDiningDetailAsync(string id)
    {
        return await _http.GetFromJsonAsync<DiningDto>($"api/resources/dining/{id}", JsonOptions);
    }

    public async Task<List<FolkActivityDto>> GetFolkActivitiesAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<List<FolkActivityDto>>($"api/resources/folkactivities?page={page}&pageSize={pageSize}", JsonOptions) ?? [];
    }
    
    public async Task<FolkActivityDto?> GetFolkActivityAsync(string id)
    {
        return await _http.GetFromJsonAsync<FolkActivityDto>($"api/resources/folkactivities/{id}", JsonOptions);
    }

    public async Task<List<BeautifulVillageDto>> GetBeautifulVillagesAsync(int page = 1, int pageSize = 20)
    {
        return await _http.GetFromJsonAsync<List<BeautifulVillageDto>>($"api/resources/beautifulvillages?page={page}&pageSize={pageSize}", JsonOptions) ?? [];
    }
    
    public async Task<BeautifulVillageDto?> GetBeautifulVillageAsync(string id)
    {
        return await _http.GetFromJsonAsync<BeautifulVillageDto>($"api/resources/beautifulvillages/{id}", JsonOptions);
    }

    public async Task<List<RecommendedResourceDto>> GetRecommendedResourcesAsync(int take = 10)
    {
        return await _http.GetFromJsonAsync<List<RecommendedResourceDto>>($"api/recommendations/resources?take={take}", JsonOptions) ?? [];
    }

    public async Task<List<RecommendedResourceDto>> SearchRecommendedResourcesAsync(string keyword, int take = 10)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return [];
        var encoded = Uri.EscapeDataString(keyword.Trim());
        return await _http.GetFromJsonAsync<List<RecommendedResourceDto>>($"api/recommendations/search/resources?keyword={encoded}&take={take}", JsonOptions) ?? [];
    }

    public async Task<List<PopularBeautifulVillageDto>> GetPopularBeautifulVillagesAsync(int take = 5)
    {
        return await _http.GetFromJsonAsync<List<PopularBeautifulVillageDto>>($"api/recommendations/villages?take={take}", JsonOptions) ?? [];
    }

    // ===== 资源新增/编辑 =====
    public async Task<AttractionDto?> CreateAttractionAsync(AttractionCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/resources/attractions", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Server returned {response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<AttractionDto>(JsonOptions);
    }

    public async Task<AttractionDto?> UpdateAttractionAsync(string id, AttractionCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/resources/attractions/{id}", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<AttractionDto>(JsonOptions);
    }

    public async Task<AccommodationDto?> CreateAccommodationAsync(AccommodationCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/resources/accommodations", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<AccommodationDto>(JsonOptions);
    }

    public async Task<AccommodationDto?> UpdateAccommodationAsync(string id, AccommodationCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/resources/accommodations/{id}", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<AccommodationDto>(JsonOptions);
    }

    public async Task<DiningDto?> CreateDiningAsync(DiningCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/resources/dining", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<DiningDto>(JsonOptions);
    }

    public async Task<DiningDto?> UpdateDiningAsync(string id, DiningCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/resources/dining/{id}", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<DiningDto>(JsonOptions);
    }

    public async Task<FolkActivityDto?> CreateFolkActivityAsync(FolkActivityCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/resources/folkactivities", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<FolkActivityDto>(JsonOptions);
    }

    public async Task<FolkActivityDto?> UpdateFolkActivityAsync(string id, FolkActivityCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/resources/folkactivities/{id}", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<FolkActivityDto>(JsonOptions);
    }

    public async Task<BeautifulVillageDto?> CreateBeautifulVillageAsync(BeautifulVillageCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/resources/beautifulvillages", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<BeautifulVillageDto>(JsonOptions);
    }

    public async Task<BeautifulVillageDto?> UpdateBeautifulVillageAsync(string id, BeautifulVillageCreateDto dto)
    {
        var response = await _http.PutAsJsonAsync($"api/resources/beautifulvillages/{id}", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<BeautifulVillageDto>(JsonOptions);
    }

    // ===== 资源删除 =====
    public async Task DeleteAttractionAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/resources/attractions/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAccommodationAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/resources/accommodations/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteDiningAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/resources/dining/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteFolkActivityAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/resources/folkactivities/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteBeautifulVillageAsync(string id)
    {
        var response = await _http.DeleteAsync($"api/resources/beautifulvillages/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ===== 资源图片 =====
    public async Task<List<ResourcePhotoDto>> GetResourcePhotosAsync(string resourceId)
    {
        return await _http.GetFromJsonAsync<List<ResourcePhotoDto>>($"api/resources/{resourceId}/photos", JsonOptions) ?? [];
    }

    public async Task<ResourcePhotoDto?> AddResourcePhotoAsync(string resourceId, string mediaId)
    {
        var response = await _http.PostAsJsonAsync($"api/resources/{resourceId}/photos", new ResourcePhotoCreateDto { MediaId = mediaId }, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
        return await response.Content.ReadFromJsonAsync<ResourcePhotoDto>(JsonOptions);
    }

    public async Task DeleteResourcePhotoAsync(string resourceId, string photoId)
    {
        var response = await _http.DeleteAsync($"api/resources/{resourceId}/photos/{photoId}");
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
    }

    // ===== 资源点评 =====
    public async Task<List<ResourceReviewDto>> GetResourceReviewsAsync(string resourceId)
    {
        return await _http.GetFromJsonAsync<List<ResourceReviewDto>>($"api/resources/{resourceId}/reviews", JsonOptions) ?? [];
    }

    public async Task<ResourceReviewDto?> UpsertResourceReviewAsync(string resourceId, ResourceReviewCreateDto dto)
    {
        var response = await _http.PostAsJsonAsync($"api/resources/{resourceId}/reviews", dto, JsonOptions);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }

        return await response.Content.ReadFromJsonAsync<ResourceReviewDto>(JsonOptions);
    }

    public async Task DeleteResourceReviewAsync(string resourceId, string reviewId)
    {
        var response = await _http.DeleteAsync($"api/resources/{resourceId}/reviews/{reviewId}");
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
    }

    public async Task TrackResourceInteractionAsync(string resourceId, string eventType = "Click", string? metadata = null)
    {
        var uri = $"api/recommendations/resources/{resourceId}/track?eventType={Uri.EscapeDataString(eventType)}";
        if (!string.IsNullOrWhiteSpace(metadata))
        {
            uri += $"&metadata={Uri.EscapeDataString(metadata)}";
        }

        var response = await _http.PostAsync(uri, null);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            throw new Exception($"Http {(int)response.StatusCode}: {msg}");
        }
    }
}
