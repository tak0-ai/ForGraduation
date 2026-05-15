using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Enums;
using RuralTourism.Api.Migrations;
using System.Security.Claims;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private static readonly string[] RuralKeywords =
    [
        "乡村", "村", "田园", "农", "农家", "采摘", "果园", "茶园", "菜园", "稻田",
        "古村", "古宅", "民宿", "农家乐", "非遗", "民俗", "研学", "生态", "湿地",
        "徒步", "骑行", "步道", "庄园", "山林", "湖畔", "水乡"
    ];

    public RecommendationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    // 推荐帖子：优先个性化（协同过滤），不足时补充热门帖子
    [HttpGet("posts")]
    public async Task<ActionResult<List<RecommendedPostDto>>> GetRecommendedPosts([FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 50);
        var userId = await GetCurrentUserIdAsync();

        // 未登录：直接返回热门帖子
        if (string.IsNullOrWhiteSpace(userId))
        {
            var fallback = await GetPopularPostsAsync(take, []);
            return Ok(fallback);
        }

        var userPostIds = await _db.Reactions
            .Where(r => r.UserId == userId && r.PostId != null && (r.Type == ReactionType.Like || r.Type == ReactionType.Bookmark))
            .Select(r => r.PostId!)
            .Distinct()
            .ToListAsync();

        // 无历史互动：回退热门帖子
        if (userPostIds.Count == 0)
        {
            var fallback = await GetPopularPostsAsync(take, []);
            return Ok(fallback);
        }

        var similarUserIds = await _db.Reactions
            .Where(r => r.PostId != null && userPostIds.Contains(r.PostId!) && r.UserId != userId)
            .Select(r => r.UserId)
            .Distinct()
            .Take(100)
            .ToListAsync();

        var scoreByPostId = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        // 汇总相似用户对候选帖子的兴趣分
        if (similarUserIds.Count > 0)
        {
            var candidateReactions = await _db.Reactions
                .Where(r => r.PostId != null && similarUserIds.Contains(r.UserId) && !userPostIds.Contains(r.PostId!))
                .Select(r => new { PostId = r.PostId!, r.Type })
                .ToListAsync();

            foreach (var item in candidateReactions)
            {
                var score = item.Type == ReactionType.Bookmark ? 5d : 3d;
                scoreByPostId[item.PostId] = scoreByPostId.GetValueOrDefault(item.PostId) + score;
            }
        }

        var selected = new List<RecommendedPostDto>();

        if (scoreByPostId.Count > 0)
        {
            var candidateIds = scoreByPostId.Keys.ToList();
            var candidates = await _db.Posts
                .Where(p => p.Status == PostStatus.Published && candidateIds.Contains(p.Id))
                .Select(p => new RecommendedPostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CoverMediaId = p.CoverMediaId,
                    CreatedAt = p.CreatedAt,
                    PublishedAt = p.PublishedAt,
                    Status = p.Status
                })
                .ToListAsync();

            selected.AddRange(candidates
                .OrderByDescending(p => scoreByPostId.GetValueOrDefault(p.Id))
                .ThenByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Take(take));
        }

        // 个性化结果不足时，用热门帖子补齐
        if (selected.Count < take)
        {
            var extra = await GetPopularPostsAsync(take - selected.Count, [.. selected.Select(x => x.Id), .. userPostIds]);
            selected.AddRange(extra);
        }

        return Ok(selected.Take(take).ToList());
    }

    // 热门乡村：按评分和热度综合排行
    [HttpGet("villages")]
    public async Task<ActionResult<List<PopularBeautifulVillageDto>>> GetPopularBeautifulVillages([FromQuery] int take = 5)
    {
        take = Math.Clamp(take, 1, 20);

        var now = DateTime.UtcNow;
        var villages = await _db.BeautifulVillages
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.Address,
                v.CoverMediaId,
                v.AverageRating,
                v.CreatedAt
            })
            .ToListAsync();

        var villageIds = villages.Select(v => v.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var relatedResourceIdsByVillage = villages.ToDictionary(
            v => v.Id,
            v => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { v.Id },
            StringComparer.OrdinalIgnoreCase);

        var taggedResources = await _db.Resources
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

        var reviewCountByResource = await _db.ResourceReviews
            .Where(r => allRelatedResourceIds.Contains(r.ResourceId))
            .GroupBy(r => r.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var eventWeights = await _db.InteractionEvents
            .Where(e => allRelatedResourceIds.Contains(e.ResourceId) && e.Timestamp >= now.AddDays(-180))
            .Select(e => new { e.ResourceId, e.EventType, e.Timestamp })
            .ToListAsync();

        var heatByResource = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var ev in eventWeights)
        {
            var weight = ev.EventType switch
            {
                InteractionEventType.Book => 6d,
                InteractionEventType.Rate => 5d,
                InteractionEventType.Share => 4d,
                InteractionEventType.Like => 3d,
                InteractionEventType.View => 2d,
                InteractionEventType.Click => 1d,
                _ => 1d
            };

            var decay = GetTimeDecay(ev.Timestamp, now);
            heatByResource[ev.ResourceId] = heatByResource.GetValueOrDefault(ev.ResourceId) + weight * decay;
        }

        var ranked = villages
            .Select(v =>
            {
                var rating = v.AverageRating <= 0 ? 5 : v.AverageRating;
                var heat = relatedResourceIdsByVillage[v.Id]
                    .Sum(resourceId => heatByResource.GetValueOrDefault(resourceId) + reviewCountByResource.GetValueOrDefault(resourceId) * 0.5);
                var total = rating * 0.7 + Math.Log10(heat + 1) * 3.0;

                return new PopularBeautifulVillageDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Address = v.Address,
                    CoverMediaId = v.CoverMediaId,
                    AverageRating = rating,
                    HeatScore = heat,
                    TotalScore = total
                };
            })
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.HeatScore)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToList();

        return Ok(ranked);
    }

    // 推荐资源：基于用户行为与标签偏好，结合评分热度排序
    [HttpGet("resources")]
    public async Task<ActionResult<List<RecommendedResourceDto>>> GetRecommendedResources([FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 50);
        var userId = await GetCurrentUserIdAsync();

        var resourceTypeMap = await BuildResourceTypeMapAsync();
        var popularFallback = await GetPopularResourcesAsync(take, [], resourceTypeMap);

        // 未登录：返回热门资源
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Ok(popularFallback);
        }

        var now = DateTime.UtcNow;
        var recentEvents = await _db.InteractionEvents
            .Where(e => e.UserId == userId && e.Timestamp >= now.AddDays(-90))
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();

        var recentReviews = await _db.ResourceReviews
            .Where(r => r.UserId == userId && r.CreatedAt >= now.AddDays(-180))
            .Select(r => new { r.ResourceId, r.Rating, r.CreatedAt })
            .ToListAsync();

        var interactedIds = new HashSet<string>(recentEvents.Select(x => x.ResourceId), StringComparer.OrdinalIgnoreCase);
        foreach (var rv in recentReviews)
        {
            interactedIds.Add(rv.ResourceId);
        }

        // 无行为数据：回退热门资源
        if (interactedIds.Count == 0)
        {
            return Ok(popularFallback);
        }

        var interactedResources = await _db.Resources
            .Where(r => interactedIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name, r.Description, r.Tags })
            .ToListAsync();

        var ruralInteractedResources = interactedResources
            .Where(x => IsRuralResource(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"), x.Name, x.Description, x.Tags))
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var tagScore = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (ruralInteractedResources.Count == 0)
        {
            return Ok(popularFallback);
        }

        // 根据事件类型给权重，并叠加时间衰减
        foreach (var ev in recentEvents.Where(e => ruralInteractedResources.ContainsKey(e.ResourceId)))
        {
            var weight = ev.EventType switch
            {
                InteractionEventType.Book or InteractionEventType.Rate => 5d,
                InteractionEventType.Share => 4d,
                InteractionEventType.Like => 3d,
                InteractionEventType.Click => 2d,
                _ => 1d
            };

            var decay = GetTimeDecay(ev.Timestamp, now);
            var resTags = ruralInteractedResources.GetValueOrDefault(ev.ResourceId)?.Tags;
            AddTagWeights(tagScore, resTags, weight * decay);
        }

        foreach (var rv in recentReviews.Where(r => ruralInteractedResources.ContainsKey(r.ResourceId)))
        {
            var weight = Math.Max(1, rv.Rating);
            var decay = GetTimeDecay(rv.CreatedAt, now);
            var resTags = ruralInteractedResources.GetValueOrDefault(rv.ResourceId)?.Tags;
            AddTagWeights(tagScore, resTags, weight * decay);
        }

        // 标签画像为空：回退热门资源
        if (tagScore.Count == 0)
        {
            return Ok(popularFallback);
        }

        var reviewCountByResource = await _db.ResourceReviews
            .GroupBy(r => r.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var candidates = await _db.Resources
            .Where(r => !interactedIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.AverageRating,
                r.CoverMediaId,
                r.Tags,
                r.CreatedAt
            })
            .Take(500)
            .ToListAsync();

        var ruralCandidates = candidates
            .Where(x => IsRuralResource(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"), x.Name, x.Description, x.Tags))
            .ToList();

        // 资源总分 = 标签匹配 + 评分 + 点评热度
        var selected = ruralCandidates
            .Select(x => new
            {
                Item = x,
                Score = CalculateTagScore(x.Tags, tagScore)
                        + Math.Max(0, x.AverageRating)
                        + reviewCountByResource.GetValueOrDefault(x.Id) * 0.2
                        + GetRuralTypeBoost(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.CreatedAt)
            .Take(take)
            .Select(x => new RecommendedResourceDto
            {
                Id = x.Item.Id,
                Name = x.Item.Name,
                Description = x.Item.Description,
                AverageRating = x.Item.AverageRating <= 0 ? 5 : x.Item.AverageRating,
                CoverMediaId = x.Item.CoverMediaId,
                ResourceType = resourceTypeMap.GetValueOrDefault(x.Item.Id, "attractions")
            })
            .ToList();

        // 个性化结果不足时，用热门资源补齐
        if (selected.Count < take)
        {
            var extra = await GetPopularResourcesAsync(take - selected.Count, [.. selected.Select(x => x.Id), .. interactedIds], resourceTypeMap);
            selected.AddRange(extra);
        }

        return Ok(selected.Take(take).ToList());
    }

    [HttpGet("search/posts")]
    public async Task<ActionResult<List<SearchRecommendedPostDto>>> SearchRecommendedPosts([FromQuery] string keyword, [FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 50);
        var query = keyword?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return Ok(new List<SearchRecommendedPostDto>());

        var userId = await GetCurrentUserIdAsync();
        var userPostIds = string.IsNullOrWhiteSpace(userId)
            ? new List<string>()
            : await _db.Reactions
                .Where(r => r.UserId == userId && r.PostId != null && (r.Type == ReactionType.Like || r.Type == ReactionType.Bookmark))
                .Select(r => r.PostId!)
                .Distinct()
                .ToListAsync();

        var collaborativeScoreByPostId = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(userId) && userPostIds.Count > 0)
        {
            var similarUserIds = await _db.Reactions
                .Where(r => r.PostId != null && userPostIds.Contains(r.PostId!) && r.UserId != userId)
                .Select(r => r.UserId)
                .Distinct()
                .Take(100)
                .ToListAsync();

            var candidateReactions = await _db.Reactions
                .Where(r => r.PostId != null && similarUserIds.Contains(r.UserId))
                .Select(r => new { PostId = r.PostId!, r.Type })
                .ToListAsync();

            foreach (var item in candidateReactions)
            {
                var score = item.Type == ReactionType.Bookmark ? 5d : 3d;
                collaborativeScoreByPostId[item.PostId] = collaborativeScoreByPostId.GetValueOrDefault(item.PostId) + score;
            }
        }

        var reactionScoreByPostId = await _db.Reactions
            .Where(r => r.PostId != null)
            .GroupBy(r => r.PostId!)
            .Select(g => new { PostId = g.Key, Score = g.Sum(r => r.Type == ReactionType.Bookmark ? 5 : 3) })
            .ToDictionaryAsync(x => x.PostId, x => x.Score, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var candidates = await _db.Posts
            .Where(p => p.Status == PostStatus.Published)
            .Select(p => new { p.Id, p.Title, p.CoverMediaId, p.CreatedAt, p.PublishedAt, p.Status })
            .ToListAsync();

        var results = candidates
            .Select(p =>
            {
                var matchScore = CalculateFuzzyMatchScore(query, p.Title);
                if (matchScore <= 0) return null;

                var recencyScore = GetRecencyBoost(p.PublishedAt ?? p.CreatedAt, now);
                var totalScore = matchScore * 100
                                 + collaborativeScoreByPostId.GetValueOrDefault(p.Id)
                                 + reactionScoreByPostId.GetValueOrDefault(p.Id) * 0.2
                                 + recencyScore;

                return new SearchRecommendedPostDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    CoverMediaId = p.CoverMediaId,
                    CreatedAt = p.CreatedAt,
                    PublishedAt = p.PublishedAt,
                    Status = p.Status,
                    Score = totalScore
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .Take(take)
            .ToList();

        return Ok(results);
    }

    [HttpGet("search/resources")]
    public async Task<ActionResult<List<SearchRecommendedResourceDto>>> SearchRecommendedResources([FromQuery] string keyword, [FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 50);
        var query = keyword?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return Ok(new List<SearchRecommendedResourceDto>());

        var resourceTypeMap = await BuildResourceTypeMapAsync();
        var userId = await GetCurrentUserIdAsync();
        var now = DateTime.UtcNow;

        var tagScore = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var interactedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var recentEvents = await _db.InteractionEvents
                .Where(e => e.UserId == userId && e.Timestamp >= now.AddDays(-90))
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            var recentReviews = await _db.ResourceReviews
                .Where(r => r.UserId == userId && r.CreatedAt >= now.AddDays(-180))
                .Select(r => new { r.ResourceId, r.Rating, r.CreatedAt })
                .ToListAsync();

            foreach (var ev in recentEvents) interactedIds.Add(ev.ResourceId);
            foreach (var rv in recentReviews) interactedIds.Add(rv.ResourceId);

            var interactedResources = await _db.Resources
                .Where(r => interactedIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Name, r.Description, r.Tags })
                .ToListAsync();

            var ruralInteractedResources = interactedResources
                .Where(x => IsRuralResource(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"), x.Name, x.Description, x.Tags))
                .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var ev in recentEvents.Where(e => ruralInteractedResources.ContainsKey(e.ResourceId)))
            {
                var weight = ev.EventType switch
                {
                    InteractionEventType.Book or InteractionEventType.Rate => 5d,
                    InteractionEventType.Share => 4d,
                    InteractionEventType.Like => 3d,
                    InteractionEventType.Click => 2d,
                    _ => 1d
                };

                var decay = GetTimeDecay(ev.Timestamp, now);
                AddTagWeights(tagScore, ruralInteractedResources.GetValueOrDefault(ev.ResourceId)?.Tags, weight * decay);
            }

            foreach (var rv in recentReviews.Where(r => ruralInteractedResources.ContainsKey(r.ResourceId)))
            {
                var decay = GetTimeDecay(rv.CreatedAt, now);
                AddTagWeights(tagScore, ruralInteractedResources.GetValueOrDefault(rv.ResourceId)?.Tags, Math.Max(1, rv.Rating) * decay);
            }
        }

        var reviewCountByResource = await _db.ResourceReviews
            .GroupBy(r => r.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var candidates = await _db.Resources
            .Select(r => new { r.Id, r.Name, r.Description, r.Address, r.AverageRating, r.CoverMediaId, r.Tags, r.CreatedAt })
            .ToListAsync();

        var results = candidates
            .Where(x => IsRuralResource(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"), x.Name, x.Description, x.Tags))
            .Select(x =>
            {
                var matchScore = CalculateFuzzyMatchScore(query, x.Name, x.Description, x.Address, x.Tags);
                if (matchScore <= 0) return null;

                var baseScore = CalculateTagScore(x.Tags, tagScore)
                                + Math.Max(0, x.AverageRating)
                                + reviewCountByResource.GetValueOrDefault(x.Id) * 0.2
                                + GetRuralTypeBoost(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"));

                return new SearchRecommendedResourceDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                    CoverMediaId = x.CoverMediaId,
                    ResourceType = resourceTypeMap.GetValueOrDefault(x.Id, "attractions"),
                    Score = matchScore * 100 + baseScore
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToList();

        return Ok(results);
    }

    // 记录资源交互行为
    [Authorize]
    [HttpPost("resources/{resourceId}/track")]
    public async Task<IActionResult> TrackResourceInteraction(string resourceId, [FromQuery] InteractionEventType eventType = InteractionEventType.Click, [FromQuery] string? metadata = null)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var exists = await _db.Resources.AnyAsync(x => x.Id == resourceId);
        if (!exists) return NotFound();

        _db.InteractionEvents.Add(new InteractionEvent
        {
            UserId = userId,
            ResourceId = resourceId,
            EventType = eventType,
            Metadata = metadata
        });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // 获取当前用户ID
    private async Task<string?> GetCurrentUserIdAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(userId)) return userId;

        var auth = await HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!auth.Succeeded || auth.Principal == null) return null;

        return auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? auth.Principal.FindFirstValue("sub");
    }

    // 热门帖子：按点赞/收藏加权排序
    private async Task<List<RecommendedPostDto>> GetPopularPostsAsync(int take, IEnumerable<string> excludePostIds)
    {
        var excludes = excludePostIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var reactionScore = await _db.Reactions
            .Where(r => r.PostId != null)
            .GroupBy(r => r.PostId!)
            .Select(g => new
            {
                PostId = g.Key,
                Score = g.Sum(r => r.Type == ReactionType.Bookmark ? 5 : 3)
            })
            .ToDictionaryAsync(x => x.PostId, x => x.Score, StringComparer.OrdinalIgnoreCase);

        var posts = await _db.Posts
            .Where(p => p.Status == PostStatus.Published && !excludes.Contains(p.Id))
            .Select(p => new RecommendedPostDto
            {
                Id = p.Id,
                Title = p.Title,
                CoverMediaId = p.CoverMediaId,
                CreatedAt = p.CreatedAt,
                PublishedAt = p.PublishedAt,
                Status = p.Status
            })
            .ToListAsync();

        return posts
            .OrderByDescending(p => reactionScore.GetValueOrDefault(p.Id))
            .ThenByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(take)
            .ToList();
    }

    // 热门资源：按评分与点评数排序
    private async Task<List<RecommendedResourceDto>> GetPopularResourcesAsync(int take, IEnumerable<string> excludeResourceIds, Dictionary<string, string>? typeMap = null)
    {
        var excludes = excludeResourceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reviewCountByResource = await _db.ResourceReviews
            .GroupBy(r => r.ResourceId)
            .Select(g => new { ResourceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ResourceId, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var resourceTypeMap = typeMap ?? await BuildResourceTypeMapAsync();

        var resources = await _db.Resources
            .Where(r => !excludes.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.AverageRating,
                r.CoverMediaId,
                r.Tags,
                r.CreatedAt
            })
            .ToListAsync();

        var ruralResources = resources
            .Where(x => IsRuralResource(resourceTypeMap.GetValueOrDefault(x.Id, "attractions"), x.Name, x.Description, x.Tags))
            .ToList();

        return ruralResources
            .OrderByDescending(x => (x.AverageRating <= 0 ? 5 : x.AverageRating) + reviewCountByResource.GetValueOrDefault(x.Id) * 0.3 + GetRuralTypeBoost(resourceTypeMap.GetValueOrDefault(x.Id, "attractions")))
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new RecommendedResourceDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                AverageRating = x.AverageRating <= 0 ? 5 : x.AverageRating,
                CoverMediaId = x.CoverMediaId,
                ResourceType = resourceTypeMap.GetValueOrDefault(x.Id, "attractions")
            })
            .ToList();
    }

    // 构建资源ID到资源类型的映射（用于前端跳转）
    private async Task<Dictionary<string, string>> BuildResourceTypeMapAsync()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in await _db.Attractions.Select(x => x.Id).ToListAsync()) map[id] = "attractions";
        foreach (var id in await _db.Accommodations.Select(x => x.Id).ToListAsync()) map[id] = "accommodations";
        foreach (var id in await _db.Dinings.Select(x => x.Id).ToListAsync()) map[id] = "dining";
        foreach (var id in await _db.FolkActivities.Select(x => x.Id).ToListAsync()) map[id] = "folk";
        foreach (var id in await _db.BeautifulVillages.Select(x => x.Id).ToListAsync()) map[id] = "beautiful-villages";

        return map;
    }

    // 计算资源标签与用户标签画像的匹配分
    private static double CalculateTagScore(string? tags, Dictionary<string, double> userTagScore)
    {
        if (string.IsNullOrWhiteSpace(tags)) return 0;
        var score = 0d;

        foreach (var tag in SplitTags(tags))
        {
            score += userTagScore.GetValueOrDefault(tag);
        }

        return score;
    }

    // 将某次行为权重累加到对应标签
    private static void AddTagWeights(Dictionary<string, double> scores, string? tags, double weight)
    {
        if (string.IsNullOrWhiteSpace(tags) || weight <= 0) return;

        foreach (var tag in SplitTags(tags))
        {
            scores[tag] = scores.GetValueOrDefault(tag) + weight;
        }
    }

    // 标签拆分与标准化（小写、去重）
    private static IEnumerable<string> SplitTags(string tags)
    {
        return tags
            .Split([',', '，', ';', '；', '|', '/', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractVillageIds(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags)) yield break;

        foreach (var tag in tags.Split([',', '，', ';', '；', '|', '/', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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

    // 判断资源是否属于乡村旅游资源
    private static bool IsRuralResource(string resourceType, string? name, string? description, string? tags)
    {
        if (string.Equals(resourceType, "beautiful-villages", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(resourceType, "folk", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ContainsRuralKeywords(name, description, tags);
    }

    // 乡村资源类型加权：美丽乡村、民俗活动优先，其余乡村内容次之
    private static double GetRuralTypeBoost(string resourceType)
    {
        return resourceType.ToLowerInvariant() switch
        {
            "beautiful-villages" => 2.0,
            "folk" => 1.6,
            "dining" => 1.0,
            "accommodations" => 1.0,
            "attractions" => 0.8,
            _ => 0.5
        };
    }

    private static bool ContainsRuralKeywords(params string?[] values)
    {
        var text = string.Join(' ', values.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (string.IsNullOrWhiteSpace(text)) return false;

        return RuralKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static double CalculateFuzzyMatchScore(string keyword, params string?[] values)
    {
        var normalizedKeyword = keyword.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKeyword)) return 0;

        var text = string.Join(' ', values.Where(x => !string.IsNullOrWhiteSpace(x))).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(text)) return 0;

        var score = 0d;
        if (text.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase))
        {
            score += 6;
        }

        foreach (var token in SplitSearchTokens(normalizedKeyword))
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                score += 2.5;
            }
            else
            {
                var overlap = CountCharacterOverlap(token, text);
                if (overlap >= Math.Min(2, token.Length))
                {
                    score += 0.8;
                }
            }
        }

        score += Math.Min(3, CountCharacterOverlap(normalizedKeyword, text) * 0.25);
        return score;
    }

    private static IEnumerable<string> SplitSearchTokens(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) yield break;

        var tokens = normalized
            .Split([',', '，', ';', '；', '|', '/', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tokens.Count > 1)
        {
            foreach (var token in tokens) yield return token;
            yield break;
        }

        yield return normalized;
        if (normalized.Length <= 2) yield break;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { normalized };
        for (var i = 0; i < normalized.Length - 1; i++)
        {
            var gram = normalized.Substring(i, 2);
            if (seen.Add(gram))
            {
                yield return gram;
            }
        }
    }

    private static int CountCharacterOverlap(string source, string target)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) return 0;
        var chars = source.Where(c => !char.IsWhiteSpace(c)).Distinct();
        return chars.Count(target.Contains);
    }

    private static double GetRecencyBoost(DateTime publishedAt, DateTime now)
    {
        var days = (now - publishedAt).TotalDays;
        if (days <= 7) return 2.0;
        if (days <= 30) return 1.0;
        if (days <= 90) return 0.5;
        return 0.1;
    }

    // 时间衰减：越近的行为影响越大
    private static double GetTimeDecay(DateTime ts, DateTime now)
    {
        var days = (now - ts).TotalDays;
        if (days <= 7) return 1.0;
        if (days <= 30) return 0.6;
        if (days <= 90) return 0.3;
        return 0.1;
    }
}