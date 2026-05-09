using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Enums;
using RuralTourism.Api.Migrations;
using RuralTourism.Api.Services;
using System.Security.Claims;

namespace RuralTourism.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;

        public ReactionsController(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        // 切换帖子点赞/收藏
        [HttpPost("post/{postId}/{type}")]
        public async Task<IActionResult> TogglePostReaction(string postId, ReactionType type)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var post = await _db.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found");

            var existing = await _db.Reactions
                .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId && r.Type == type);

            bool isAdded;
            if (existing != null)
            {
                _db.Reactions.Remove(existing);
                isAdded = false;
            }
            else
            {
                var reaction = new Reaction
                {
                    PostId = postId,
                    UserId = userId,
                    Type = type
                };
                _db.Reactions.Add(reaction);
                isAdded = true;
            }

            await _db.SaveChangesAsync();

            // 通知逻辑
            if (isAdded && post.AuthorId != userId)
            {
                // 加载触发用户信息用于通知文案
                var reactor = await _db.AppUsers.FindAsync(userId);
                string reactorName = reactor?.Nickname ?? reactor?.UserName ?? "Someone";

                if (type == ReactionType.Like)
                {
                    await _notifications.AddNotificationAsync(post.AuthorId, "点赞", "赞了你的文章", postId, userId, default);
                }
                else if (type == ReactionType.Bookmark)
                {
                    await _notifications.AddNotificationAsync(post.AuthorId, "收藏", "收藏了你的文章", postId, userId, default);
                }
            }

            return Ok(new { isAdded });
        }

        // 切换评论点赞
        [HttpPost("comment/{commentId}/{type}")]
        public async Task<IActionResult> ToggleCommentReaction(string commentId, ReactionType type)
        {
            if (type != ReactionType.Like) return BadRequest("Only Like is supported for comments currently.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var comment = await _db.Comments.Include(c => c.ParentComment).FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) return NotFound("Comment not found");

            var existing = await _db.Reactions
                .FirstOrDefaultAsync(r => r.CommentId == commentId && r.UserId == userId && r.Type == type);

            bool isAdded;
            if (existing != null)
            {
                _db.Reactions.Remove(existing);
                isAdded = false;
            }
            else
            {
                var reaction = new Reaction
                {
                    CommentId = commentId,
                    UserId = userId,
                    Type = type
                };
                _db.Reactions.Add(reaction);
                isAdded = true;
            }

            await _db.SaveChangesAsync();

            // 通知逻辑
            if (isAdded && comment.AuthorId != userId)
            {
                var reactor = await _db.AppUsers.FindAsync(userId);
                string reactorName = reactor?.Nickname ?? reactor?.UserName ?? "Someone";

                // 当前为评论点赞通知
                await _notifications.AddNotificationAsync(comment.AuthorId, "点赞", "赞了你的评论", comment.PostId, userId, default);
            }

            return Ok(new { isAdded });
        }
    }
}
