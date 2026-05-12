using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System.Security.Claims;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TourPlansController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public TourPlansController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TourPlanDto>> CreateTourPlan([FromBody] TourPlanUpsertDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var tourPlan = new TourPlan
        {
            Title = dto.Title,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            RouteMode = dto.RouteMode,
            ReturnToStart = dto.ReturnToStart,
            StartAddress = dto.StartAddress,
            Waypoints = dto.Waypoints
        };

        _db.TourPlans.Add(tourPlan);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTourPlan), new { id = tourPlan.Id }, MapToDto(tourPlan));
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<TourPlanDto>> GetTourPlan(string id)
    {
        var tourPlan = await _db.TourPlans.FindAsync(id);
        if (tourPlan == null) return NotFound();
        return Ok(MapToDto(tourPlan));
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<TourPlanDto>>> GetUserTourPlans()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var tourPlans = await _db.TourPlans
            .Where(t => t.CreatedById == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tourPlans.Select(MapToDto));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<List<TourPlanDto>>> GetMyTourPlans()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var tourPlans = await _db.TourPlans
            .Where(t => t.CreatedById == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tourPlans.Select(MapToDto));
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<TourPlanDto>> UpdateTourPlan(string id, [FromBody] TourPlanUpsertDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var tourPlan = await _db.TourPlans.FindAsync(id);
        if (tourPlan == null) return NotFound();
        if (tourPlan.CreatedById != userId) return Forbid();

        tourPlan.Title = dto.Title;
        tourPlan.RouteMode = dto.RouteMode;
        tourPlan.ReturnToStart = dto.ReturnToStart;
        tourPlan.StartAddress = dto.StartAddress;
        tourPlan.Waypoints = dto.Waypoints;

        await _db.SaveChangesAsync();
        return Ok(MapToDto(tourPlan));
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTourPlan(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var tourPlan = await _db.TourPlans.FindAsync(id);
        if (tourPlan == null) return NotFound();
        if (tourPlan.CreatedById != userId) return Forbid();

        _db.TourPlans.Remove(tourPlan);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private TourPlanDto MapToDto(TourPlan tourPlan)
    {
        return new TourPlanDto
        {
            Id = tourPlan.Id,
            Title = tourPlan.Title,
            CreatedAt = tourPlan.CreatedAt,
            RouteMode = tourPlan.RouteMode,
            ReturnToStart = tourPlan.ReturnToStart,
            StartAddress = tourPlan.StartAddress,
            Waypoints = tourPlan.Waypoints
        };
    }
}

public class TourPlanDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string RouteMode { get; set; } = "driving";
    public bool ReturnToStart { get; set; }
    public string StartAddress { get; set; } = string.Empty;
    public List<string> Waypoints { get; set; } = [];
}

public class TourPlanUpsertDto
{
    public string Title { get; set; } = string.Empty;
    public string StartAddress { get; set; } = string.Empty;
    public List<string> Waypoints { get; set; } = [];
    public string RouteMode { get; set; } = "driving";
    public bool ReturnToStart { get; set; }
}