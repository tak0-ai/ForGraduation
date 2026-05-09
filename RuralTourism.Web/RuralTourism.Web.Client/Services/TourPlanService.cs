using System.Net.Http.Json;
using RuralTourism.Web.Client.Models;

namespace RuralTourism.Web.Client.Services;

public class TourPlanService
{
    private readonly HttpClient _http;

    public TourPlanService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TourPlanDto>> GetMyPlansAsync()
    {
        var res = await _http.GetAsync("api/tourplans/me");
        if (!res.IsSuccessStatusCode) return [];
        return await res.Content.ReadFromJsonAsync<List<TourPlanDto>>() ?? [];
    }

    public async Task<TourPlanDto?> GetByIdAsync(string id)
    {
        var res = await _http.GetAsync($"api/tourplans/{id}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<TourPlanDto>();
    }

    public async Task<TourPlanDto?> CreateAsync(TourPlanUpsertDto dto)
    {
        var res = await _http.PostAsJsonAsync("api/tourplans", dto);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<TourPlanDto>();
    }

    public async Task<TourPlanDto?> UpdateAsync(string id, TourPlanUpsertDto dto)
    {
        var res = await _http.PutAsJsonAsync($"api/tourplans/{id}", dto);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<TourPlanDto>();
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var res = await _http.DeleteAsync($"api/tourplans/{id}");
        return res.IsSuccessStatusCode;
    }
}
