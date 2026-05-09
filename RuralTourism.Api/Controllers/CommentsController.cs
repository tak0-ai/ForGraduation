using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Migrations;
using System.Security.Claims;

using RuralTourism.Api.Services;

using RuralTourism.Api.Enums;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public CommentsController(ApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpGet("post/{postId}")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(string postId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var allComments = await _db.Comments
            .Include(c => c.Author)
            .Include(c => c.Reactions)
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var dtoList = allComments.Select(c => new CommentDto
        {
            Id = c.Id,
            PostId = c.PostId,
            AuthorId = c.AuthorId,
            // 格式化 UserNo 为6位默认显示
            AuthorUserNo = c.Author != null ? c.Author.UserNo.ToString("D6") : "000000",
            AuthorName = c.Author?.Nickname ?? c.Author?.UserName ?? "匿名用户",
            AuthorAvatarUrl = c.Author?.AvatarUrl,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            ParentCommentId = c.ParentCommentId,
            LikeCount = c.Reactions.Count(r => r.Type == ReactionType.Like),
            IsLiked = !string.IsNullOrEmpty(currentUserId) && c.Reactions.Any(r => r.UserId == currentUserId && r.Type == ReactionType.Like)
        }).ToList();

        // 简单的两层结构构建：顶级评论和它们的直接回复
        var rootComments = dtoList.Where(c => c.ParentCommentId == null).ToList();
        var replyComments = dtoList.Where(c => c.ParentCommentId != null).ToList();

        foreach (var root in rootComments)
        {
            // 找到该评论的所有回复（如果是多级，这里可以递归，但通常两级够用）
            root.Replies = replyComments
                .Where(r => r.ParentCommentId == root.Id)
                .OrderBy(r => r.CreatedAt)
                .ToList();
        }

        return Ok(rootComments);
    }

    [Authorize]
    [HttpPost("post/{postId}")]
    public async Task<ActionResult<CommentDto>> CreateComment(string postId, [FromBody] CommentCreateDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return NotFound("文章不存在");

        var comment = new Comment
        {
            PostId = postId,
            AuthorId = userId,
            Content = dto.Content,
            ParentCommentId = dto.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        // 重新加载以便包含作者信息
        await _db.Entry(comment).Reference(c => c.Author).LoadAsync();

        try
        {
            if (!string.IsNullOrEmpty(dto.ReplyToUserId) && dto.ReplyToUserId != userId)
            {
                 // Reply to specific user
                 var preview = comment.Content.Length > 20 ? comment.Content.Substring(0, 20) + "..." : comment.Content;
                 await _notifications.AddNotificationAsync(dto.ReplyToUserId, "回复", $"{preview}\n回复了你的评论", postId, userId, default);
            }
            else if (!string.IsNullOrEmpty(dto.ParentCommentId))
            {
                 var parent = await _db.Comments.FindAsync(dto.ParentCommentId);
                 if (parent != null && parent.AuthorId != userId)
                 {
                     var preview = comment.Content.Length > 20 ? comment.Content.Substring(0, 20) + "..." : comment.Content;
                     await _notifications.AddNotificationAsync(parent.AuthorId, "回复", $"{preview}\n回复了你的评论", postId, userId, default);
                 }
            }
            else
            {
                 if (post.AuthorId != userId)
                 {
                     var preview = comment.Content.Length > 20 ? comment.Content.Substring(0, 20) + "..." : comment.Content;
                     await _notifications.AddNotificationAsync(post.AuthorId, "评论", $"{preview}\n评论了你的文章《{post.Title}》", postId, userId, default);
                 }
            }
        }
        catch { }

        return Ok(new CommentDto
        {
            Id = comment.Id,
            PostId = comment.PostId,
            AuthorId = comment.AuthorId,
            AuthorUserNo = comment.Author != null ? comment.Author.UserNo.ToString("D6") : "000000",
            AuthorName = comment.Author?.Nickname ?? comment.Author?.UserName ?? "匿名用户",
            AuthorAvatarUrl = comment.Author?.AvatarUrl,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            ParentCommentId = comment.ParentCommentId
        });
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var comment = await _db.Comments.FindAsync(id);

        if (comment == null) return NotFound();
        if (comment.AuthorId != userId && !User.IsInRole("Admin")) return Forbid();

        _db.Comments.Remove(comment);
        
        // 如果有子评论且配置了 Restrict，可能需要手动处理或改为级联
        // ApplicationDbContext 中配置的是 Restrict
        var children = await _db.Comments.Where(c => c.ParentCommentId == id).ToListAsync();
        _db.Comments.RemoveRange(children);

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
