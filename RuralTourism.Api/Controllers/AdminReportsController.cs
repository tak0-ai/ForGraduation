using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Enums;
using RuralTourism.Api.Migrations;
using System.Globalization;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminReportsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminReportOverviewDto>> GetOverview([FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 100);

        var posts = await BuildPostReportsAsync(take);
        var villages = await BuildVillageReportsAsync(take);
        var attractions = await BuildAttractionReportsAsync(take);

        var summary = new AdminReportSummaryDto
        {
            Posts = posts.Count,
            Villages = villages.Count,
            Attractions = attractions.Count,
            TotalViews = posts.Sum(x => x.Views) + villages.Sum(x => x.Views) + attractions.Sum(x => x.Views) + villages.Sum(x => x.ChildResourceViews),
            TotalInteractions = posts.Sum(x => x.Comments + x.Likes + x.Bookmarks + x.Shares) + villages.Sum(x => x.Reviews + x.ChildResourceReviews + x.Shares) + attractions.Sum(x => x.Reviews + x.Shares),
            TotalHeat = posts.Sum(x => x.HeatScore) + villages.Sum(x => x.HeatScore) + attractions.Sum(x => x.HeatScore)
        };

        return Ok(new AdminReportOverviewDto
        {
            Summary = summary,
            Posts = posts,
            Villages = villages,
            Attractions = attractions
        });
    }

    private async Task<List<AdminPostReportDto>> BuildPostReportsAsync(int take)
    {
        var posts = await _db.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.CreatedAt,
                AuthorName = p.Author != null ? (p.Author.Nickname ?? p.Author.UserName) : null
            })
            .ToListAsync();

        var postIds = posts.Select(x => x.Id).ToList();
        var interactionBuckets = await GetInteractionBucketsAsync(postIds);

        var commentCounts = await _db.Comments
            .AsNoTracking()
            .Where(c => postIds.Contains(c.PostId))
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var reactionBuckets = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.PostId != null && postIds.Contains(r.PostId))
            .GroupBy(r => r.PostId!)
            .Select(g => new
            {
                PostId = g.Key,
                Likes = g.Count(x => x.Type == ReactionType.Like),
                Bookmarks = g.Count(x => x.Type == ReactionType.Bookmark)
            })
            .ToDictionaryAsync(x => x.PostId, x => new { x.Likes, x.Bookmarks }, StringComparer.OrdinalIgnoreCase);

        return posts
            .Select(p =>
            {
                var bucket = interactionBuckets.GetValueOrDefault(p.Id, InteractionBucket.Empty);
                var comments = commentCounts.GetValueOrDefault(p.Id);
                var likeCount = reactionBuckets.GetValueOrDefault(p.Id)?.Likes ?? 0;
                var bookmarkCount = reactionBuckets.GetValueOrDefault(p.Id)?.Bookmarks ?? 0;
                var shareCount = bucket.Get(InteractionEventType.Share);
                var views = bucket.Get(InteractionEventType.View);
                var heat = views + comments * 2d + likeCount * 3d + bookmarkCount * 4d + shareCount * 4d;

                return new AdminPostReportDto
                {
                    Id = p.Id,
                    Title = string.IsNullOrWhiteSpace(p.Title) ? "?ùùùùùùùù" : p.Title,
                    AuthorName = p.AuthorName,
                    CreatedAt = p.CreatedAt,
                    Views = views,
                    Comments = comments,
                    Likes = likeCount,
                    Bookmarks = bookmarkCount,
                    Shares = shareCount,
                    HeatScore = heat
                };
            })
            .OrderByDescending(x => x.HeatScore)
            .ThenByDescending(x => x.Views)
            .Take(take)
            .ToList();
    }

    private async Task<List<AdminVillageReportDto>> BuildVillageReportsAsync(int take)
    {
        var villages = await _db.Resources
            .AsNoTracking()
            .OfType<BeautifulVillage>()
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.Address,
                v.CreatedAt
            })
            .ToListAsync();

        var villageIds = villages.Select(v => v.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var relatedResourceIdsByVillage = villages.ToDictionary(
            v => v.Id,
            v => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { v.Id },
            StringComparer.OrdinalIgnoreCase);

        var taggedResources = await _db.Resources
            .AsNoTracking()
            .Where(r => !string.IsNullOrWhiteSpace(r.Tags))
            .Select(r => new { r.Id, r.Tags })
            .ToListAsync();

        foreach (var resource in taggedResources)
        {
            foreach (var villageId in ExtractVillageIds(resource.Tags).Where(villageIds.Contains))
            {
                relatedResourceIdsByVillage[villageId].Add(resource.Id);
            }
        }

        var allRelatedResourceIds = relatedResourceIdsByVillage.Values
            .SelectMany(x => x)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var interactionBuckets = await GetInteractionBucketsAsync(allRelatedResourceIds);

        var reviewCounts = await _db.ResourceReviews
            .AsNoTracking()
            .Where(r => allRelatedResourceIds.Contains(r.ResourceId))
            .GroupBy(r => r.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        return villages
            .Select(v =>
            {
                var directBucket = interactionBuckets.GetValueOrDefault(v.Id, InteractionBucket.Empty);
                var childResourceIds = relatedResourceIdsByVillage[v.Id].Where(x => !string.Equals(x, v.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                var childViews = childResourceIds.Sum(id => interactionBuckets.GetValueOrDefault(id, InteractionBucket.Empty).Get(InteractionEventType.View));
                var childShares = childResourceIds.Sum(id => interactionBuckets.GetValueOrDefault(id, InteractionBucket.Empty).Get(InteractionEventType.Share));
                var childReviews = childResourceIds.Sum(id => reviewCounts.GetValueOrDefault(id));
                var directReviews = reviewCounts.GetValueOrDefault(v.Id);
                var directViews = directBucket.Get(InteractionEventType.View);
                var directShares = directBucket.Get(InteractionEventType.Share);
                var heat = directViews + childViews + (directReviews + childReviews) * 2d + (directShares + childShares) * 4d;

                return new AdminVillageReportDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Address = v.Address,
                    CreatedAt = v.CreatedAt,
                    Views = directViews,
                    Reviews = directReviews,
                    ChildResourceViews = childViews,
                    ChildResourceReviews = childReviews,
                    Shares = directShares + childShares,
                    HeatScore = heat
                };
            })
            .OrderByDescending(x => x.HeatScore)
            .ThenByDescending(x => x.Views + x.ChildResourceViews)
            .Take(take)
            .ToList();
    }

    private async Task<List<AdminAttractionReportDto>> BuildAttractionReportsAsync(int take)
    {
        var attractions = await _db.Resources
            .AsNoTracking()
            .OfType<Attraction>()
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Address,
                a.CreatedAt
            })
            .ToListAsync();

        var attractionIds = attractions.Select(x => x.Id).ToList();
        var interactionBuckets = await GetInteractionBucketsAsync(attractionIds);

        var reviewCounts = await _db.ResourceReviews
            .AsNoTracking()
            .Where(r => attractionIds.Contains(r.ResourceId))
            .GroupBy(r => r.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        return attractions
            .Select(a =>
            {
                var bucket = interactionBuckets.GetValueOrDefault(a.Id, InteractionBucket.Empty);
                var views = bucket.Get(InteractionEventType.View);
                var shares = bucket.Get(InteractionEventType.Share);
                var reviews = reviewCounts.GetValueOrDefault(a.Id);
                var heat = views + reviews * 2d + shares * 4d;

                return new AdminAttractionReportDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Address = a.Address,
                    CreatedAt = a.CreatedAt,
                    Views = views,
                    Reviews = reviews,
                    Shares = shares,
                    HeatScore = heat
                };
            })
            .OrderByDescending(x => x.HeatScore)
            .ThenByDescending(x => x.Views)
            .Take(take)
            .ToList();
    }

    private async Task<Dictionary<string, InteractionBucket>> GetInteractionBucketsAsync(IEnumerable<string> resourceIds)
    {
        var ids = resourceIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var map = ids.ToDictionary(x => x, _ => new InteractionBucket(), StringComparer.OrdinalIgnoreCase);
        if (ids.Count == 0) return map;

        var rows = await _db.InteractionEvents
            .AsNoTracking()
            .Where(e => ids.Contains(e.ResourceId))
            .GroupBy(e => new { e.ResourceId, e.EventType })
            .Select(g => new { g.Key.ResourceId, g.Key.EventType, Count = g.Count() })
            .ToListAsync();

        foreach (var row in rows)
        {
            if (!map.TryGetValue(row.ResourceId, out var bucket))
            {
                bucket = new InteractionBucket();
                map[row.ResourceId] = bucket;
            }

            bucket.Set(row.EventType, row.Count);
        }

        return map;
    }

    private static IEnumerable<string> ExtractVillageIds(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) yield break;

        foreach (var tag in tags.Split([',', 'ùù', ';', 'ùù', '|', '/', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (tag.StartsWith("village:", StringComparison.OrdinalIgnoreCase))
            {
                var villageId = tag["village:".Length..].Trim();
                if (!string.IsNullOrWhiteSpace(villageId))
                {
                    yield return villageId;
                }
            }
        }
    }

    private sealed class InteractionBucket
    {
        public static InteractionBucket Empty { get; } = new();

        private readonly Dictionary<InteractionEventType, int> _counts = new();

        public int Get(InteractionEventType type) => _counts.GetValueOrDefault(type);

        public void Set(InteractionEventType type, int count) => _counts[type] = count;
    }
}