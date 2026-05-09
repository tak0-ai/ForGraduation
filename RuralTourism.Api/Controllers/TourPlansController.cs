using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System.Security.Claims;
using System.Text.Json;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TourPlansController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TourPlansController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<TourPlanDto>>> GetMyPlans()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var items = await _db.TourPlans
            .Where(x => x.CreatedById == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TourPlanDto>> GetById(string id)
    {
        var entity = await _db.TourPlans.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null) return NotFound();
        return Ok(ToDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<TourPlanDto>> Create([FromBody] TourPlanUpsertDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("行程名称不能为空");
        if (string.IsNullOrWhiteSpace(dto.StartAddress)) return BadRequest("起始地不能为空");

        var entity = new TourPlan
        {
            Title = dto.Title.Trim(),
            CreatedById = userId,
            AutoRouteData = JsonSerializer.Serialize(ToRouteData(dto))
        };

        _db.TourPlans.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(ToDto(entity));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TourPlanDto>> Update(string id, [FromBody] TourPlanUpsertDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var entity = await _db.TourPlans.FirstOrDefaultAsync(x => x.Id == id && x.CreatedById == userId);
        if (entity == null) return NotFound();

        entity.Title = string.IsNullOrWhiteSpace(dto.Title) ? entity.Title : dto.Title.Trim();
        entity.AutoRouteData = JsonSerializer.Serialize(ToRouteData(dto));
        await _db.SaveChangesAsync();
        return Ok(ToDto(entity));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var entity = await _db.TourPlans.FirstOrDefaultAsync(x => x.Id == id && x.CreatedById == userId);
        if (entity == null) return NotFound();

        _db.TourPlans.Remove(entity);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static TourPlanDto ToDto(TourPlan entity)
    {
        var route = ParseRouteData(entity.AutoRouteData);
        return new TourPlanDto
        {
            Id = entity.Id,
            Title = entity.Title,
            CreatedAt = entity.CreatedAt,
            RouteMode = route.RouteMode,
            ReturnToStart = route.ReturnToStart,
            StartAddress = route.StartAddress,
            Waypoints = route.Waypoints
        };
    }

    private static TourRouteData ToRouteData(TourPlanUpsertDto dto) => new()
    {
        StartAddress = dto.StartAddress.Trim(),
        Waypoints = dto.Waypoints.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
        RouteMode = dto.RouteMode.Equals("transit", StringComparison.OrdinalIgnoreCase) ? "transit" : "driving",
        ReturnToStart = dto.ReturnToStart
    };

    private static TourRouteData ParseRouteData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new TourRouteData();
        try
        {
            return JsonSerializer.Deserialize<TourRouteData>(json) ?? new TourRouteData();
        }
        catch
        {
            return new TourRouteData();
        }
    }

    private sealed class TourRouteData
    {
        public string StartAddress { get; set; } = string.Empty;
        public List<string> Waypoints { get; set; } = [];
        public string RouteMode { get; set; } = "driving";
        public bool ReturnToStart { get; set; }
    }
}
