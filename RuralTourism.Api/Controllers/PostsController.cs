using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using RuralTourism.Api.Hubs;

using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<PostHub> _hubContext;

        public PostsController(ApplicationDbContext db, IWebHostEnvironment env, IHubContext<PostHub> hubContext)
        {
            _db = db;
            _env = env;
            _hubContext = hubContext;
        }

        [Authorize]
        [HttpPost("{id}/track")]
        public async Task<IActionResult> TrackPostInteraction(string id, [FromQuery] InteractionEventType eventType = InteractionEventType.View, [FromQuery] string? metadata = null)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var exists = await _db.Posts.AnyAsync(x => x.Id == id);
            if (!exists) return NotFound();

            _db.InteractionEvents.Add(new InteractionEvent
            {
                UserId = userId,
                ResourceId = id,
                EventType = eventType,
                Metadata = metadata
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }


        [HttpGet]//获取已发布的文章列表
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var items = await _db.Posts
                .Where(p => p.Status != Enums.PostStatus.Archived && p.Status == Enums.PostStatus.Published)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    coverMediaId = p.CoverMediaId,
                    status = p.Status,
                    createdAt = p.CreatedAt,
                    publishedAt = p.PublishedAt
                })
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]//根据 ID 获取单篇文章
        public async Task<IActionResult> Get(string id)
        {
            var post = await _db.Posts
                .Include(p => p.Blocks)
                .Include(p => p.Reactions)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (post == null) return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // If the post is a draft or archived, only the author or admins should access it
            if (post.Status != Enums.PostStatus.Published)
            {
                if (string.IsNullOrWhiteSpace(currentUserId) || (post.AuthorId != currentUserId && !User.IsInRole("Admin")))
                {
                    return Forbid();
                }
            }
            
            var likeCount = post.Reactions.Count(r => r.Type == ReactionType.Like);
            var collectCount = post.Reactions.Count(r => r.Type == ReactionType.Bookmark);
            var isLiked = !string.IsNullOrEmpty(currentUserId) && post.Reactions.Any(r => r.UserId == currentUserId && r.Type == ReactionType.Like);
            var isCollected = !string.IsNullOrEmpty(currentUserId) && post.Reactions.Any(r => r.UserId == currentUserId && r.Type == ReactionType.Bookmark);

            var result = new
            {
                id = post.Id,
                authorId = post.AuthorId,
                title = post.Title,
                coverMediaId = post.CoverMediaId,
                status = post.Status,
                createdAt = post.CreatedAt,
                publishedAt = post.PublishedAt,
                blocks = post.Blocks.OrderBy(b => b.Order).Select(b => new
                {
                    id = b.Id,
                    order = b.Order,
                    type = b.Type,
                    content = b.Content,
                    caption = b.Caption
                }).ToList(),
                likeCount = likeCount,
                collectCount = collectCount,
                isLiked = isLiked,
                isCollected = isCollected,
                reviewComment = post.HiddenReason
            };

            return Ok(result);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> MyPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var items = await _db.Posts
                .Where(p => p.AuthorId == userId)
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    coverMediaId = p.CoverMediaId,
                    status = p.Status,
                    createdAt = p.CreatedAt,
                    publishedAt = p.PublishedAt,
                    rejectReason = p.HiddenReason
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("pending-reviews")]
        public async Task<IActionResult> PendingReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 200);

            var items = await _db.Posts
                .Where(p => p.Status == Enums.PostStatus.PendingReview)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    coverMediaId = p.CoverMediaId,
                    status = p.Status,
                    createdAt = p.CreatedAt,
                    publishedAt = p.PublishedAt,
                    rejectReason = p.HiddenReason
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize]
        [HttpGet("collections")]
        public async Task<IActionResult> MyCollections([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var query = _db.Reactions
                .Where(r => r.UserId == userId && r.Type == ReactionType.Bookmark && r.PostId != null)
                .Include(r => r.Post)
                    .ThenInclude(p => p!.Blocks)
                .Where(r => r.Post != null && r.Post.Status == PostStatus.Published)
                .OrderByDescending(r => r.CreatedAt);

            var reactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = reactions.Select(r =>
            {
                var p = r.Post!;
                var cover = p.CoverMediaId;
                
                if (string.IsNullOrEmpty(cover))
                {
                    var firstImg = p.Blocks
                        .Where(b => b.Type == BlockType.Image)
                        .OrderBy(b => b.Order)
                        .FirstOrDefault()?.Content;
                    cover = firstImg;
                }

                if (string.IsNullOrEmpty(cover))
                {
                    cover = "https://placehold.co/600x400?text=No+Image";
                }

                return new
                {
                    id = p.Id,
                    title = p.Title,
                    coverMediaId = cover,
                    status = p.Status,
                    createdAt = p.CreatedAt,
                    publishedAt = p.PublishedAt
                };
            });

            return Ok(items);
        }

        [Authorize]
        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Create([FromForm] string postJson, IFormFile? coverImage)
        {
            PostCreateDto dto;
            if (!string.IsNullOrEmpty(postJson))
            {
                dto = JsonSerializer.Deserialize<PostCreateDto>(postJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new PostCreateDto();
            }
            else
            {
                dto = new PostCreateDto();
            }

            string? coverMediaId = null;
            if (coverImage != null)
            {
                var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(coverImage.FileName)}";
                var filePath = Path.Combine(uploads, fileName);
                using var fs = System.IO.File.Create(filePath);
                await coverImage.CopyToAsync(fs);
                coverMediaId = $"/uploads/{fileName}";
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { error = "未认证的用户，无法创建文章。" });
            }
            var post = new Post
            {
                Id = Guid.NewGuid().ToString(),
                AuthorId = userId,
                Title = dto.Title,
                CoverMediaId = coverMediaId ?? dto.CoverMediaId,
                Status = ResolveSubmitStatus(dto.IsDraft, User.IsInRole("Admin")),
                CreatedAt = DateTime.UtcNow,
                PublishedAt = dto.IsDraft || !User.IsInRole("Admin") ? null : DateTime.UtcNow
            };

            // map blocks
            int order = 0;
            foreach (var b in dto.Blocks.OrderBy(b => b.Order))
            {
                post.Blocks.Add(new PostBlock
                {
                    Id = b.Id ?? Guid.NewGuid().ToString(),
                    PostId = post.Id,
                    Order = order++,
                    Type = b.Type,
                    Content = b.Content ?? string.Empty,
                    Caption = b.Caption
                });
            }

            _db.Posts.Add(post);
            await _db.SaveChangesAsync();

            // Notify list group that a new post was created
            await _hubContext.Clients.Group("PostList").SendAsync("PostCreated", post.Id);

            // Return a simple DTO to avoid serializing EF navigation properties

            var result = new
            {
                id = post.Id,
                title = post.Title,
                coverMediaId = post.CoverMediaId,
                createdAt = post.CreatedAt,
                publishedAt = post.PublishedAt,
                status = post.Status,
                blocks = post.Blocks.Select(b => new { id = b.Id, order = b.Order, type = b.Type, content = b.Content, caption = b.Caption }).OrderBy(b => b.order).ToList()
            };

            return CreatedAtAction(nameof(Get), new { id = post.Id }, result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromForm] string? postJson, IFormFile? coverImage)
        {
            var post = await _db.Posts.Include(p => p.Blocks).FirstOrDefaultAsync(p => p.Id == id);
            if (post == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (post.AuthorId != userId && !User.IsInRole("Admin")) return Forbid();

            if (post.Status == Enums.PostStatus.PendingReview && !User.IsInRole("Admin"))
            {
                return BadRequest(new { error = "帖子正在审核中，暂不可编辑。" });
            }

            PostCreateDto dto = new();
            if (!string.IsNullOrEmpty(postJson))
            {
                dto = JsonSerializer.Deserialize<PostCreateDto>(postJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new();
            }

            post.Title = dto.Title;
            post.Status = ResolveSubmitStatus(dto.IsDraft, User.IsInRole("Admin"));
            post.UpdatedAt = DateTime.UtcNow;
            post.PublishedAt = post.Status == Enums.PostStatus.Published
                ? (post.PublishedAt ?? DateTime.UtcNow)
                : null;
            if (post.Status == Enums.PostStatus.PendingReview || post.Status == Enums.PostStatus.Published)
            {
                post.HiddenReason = null;
            }

            // 简单替换 blocks：删除旧的、插入新的
            post.Blocks.Clear();
            int order = 0;
            foreach (var b in dto.Blocks.OrderBy(b => b.Order))
            {
                post.Blocks.Add(new PostBlock
                {
                    Id = b.Id ?? Guid.NewGuid().ToString(),
                    PostId = post.Id,
                    Order = order++,
                    Type = b.Type,
                    Content = b.Content ?? string.Empty,
                    Caption = b.Caption
                });
            }

            await _db.SaveChangesAsync();
            await _hubContext.Clients.Group(id).SendAsync("PostUpdated", id);
            await _hubContext.Clients.Group("PostList").SendAsync("PostUpdated", id);
            return NoContent();

        }


        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (post.AuthorId != userId && !User.IsInRole("Admin")) return Forbid();

            // 软删或硬删，根据需求
            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            // Real-time update: notify clients
            await _hubContext.Clients.Group(id).SendAsync("PostDeleted", id);
            await _hubContext.Clients.Group("PostList").SendAsync("PostDeleted", id);

            return NoContent();
        }

        [Authorize]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> SetStatus(string id, [FromQuery] Enums.PostStatus status)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (post.AuthorId != userId && !User.IsInRole("Admin")) return Forbid();

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && status == Enums.PostStatus.Published)
            {
                return BadRequest(new { error = "普通用户发布内容需要管理员审核，请提交为待审核状态。" });
            }

            post.Status = status;
            post.UpdatedAt = DateTime.UtcNow;
            post.PublishedAt = status == Enums.PostStatus.Published
                ? (post.PublishedAt ?? DateTime.UtcNow)
                : null;
            if (status == Enums.PostStatus.Published || status == Enums.PostStatus.PendingReview)
            {
                post.HiddenReason = null;
            }
            
            await _db.SaveChangesAsync();

            // Notify clients that the post status changed (which effectively means update or delete from list)
            await _hubContext.Clients.Group(id).SendAsync("PostUpdated", id);
            await _hubContext.Clients.Group("PostList").SendAsync("PostUpdated", id);

            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/review")]
        public async Task<IActionResult> Review(string id, [FromBody] PostReviewActionDto dto)
        {
            var post = await _db.Posts.FindAsync(id);
            if (post == null) return NotFound();

            if (post.Status != Enums.PostStatus.PendingReview)
            {
                return BadRequest(new { error = "仅待审核状态的帖子可执行审核操作。" });
            }

            if (dto.Approve)
            {
                post.Status = Enums.PostStatus.Published;
                post.PublishedAt ??= DateTime.UtcNow;
                post.HiddenReason = null;
            }
            else
            {
                var reason = dto.Reason?.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return BadRequest(new { error = "驳回时请填写修改原因。" });
                }

                post.Status = Enums.PostStatus.Draft;
                post.PublishedAt = null;
                post.HiddenReason = reason;
            }

            post.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _hubContext.Clients.Group(id).SendAsync("PostUpdated", id);
            await _hubContext.Clients.Group("PostList").SendAsync("PostUpdated", id);

            return NoContent();
        }

        private static Enums.PostStatus ResolveSubmitStatus(bool isDraft, bool isAdmin)
        {
            if (isDraft) return Enums.PostStatus.Draft;
            return isAdmin ? Enums.PostStatus.Published : Enums.PostStatus.PendingReview;
        }


    }
}

