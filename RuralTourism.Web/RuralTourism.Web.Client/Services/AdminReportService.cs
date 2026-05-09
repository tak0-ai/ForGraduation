using System.Net.Http.Json;
using System.Text.Json;
using RuralTourism.Web.Client.Models;

namespace RuralTourism.Web.Client.Services;

public sealed class AdminReportService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AdminReportService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AdminReportOverviewDto?> GetOverviewAsync(int take = 20)
    {
        try
        {
            return await _http.GetFromJsonAsync<AdminReportOverviewDto>($"api/admin/reports/overview?take={take}", JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
