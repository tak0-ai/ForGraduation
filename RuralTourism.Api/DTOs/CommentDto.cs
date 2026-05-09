using System;
using System.Collections.Generic;

namespace RuralTourism.Api.DTOs;

public class CommentDto
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    // 新增 UserNo，用于前端可能的展示或查询
    public string AuthorUserNo { get; set; } = null!;
    public string AuthorName { get; set; } = null!;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? ParentCommentId { get; set; }
    public List<CommentDto> Replies { get; set; } = [];
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }
}

public class CommentCreateDto
{
    public string Content { get; set; } = null!;
    public string? ParentCommentId { get; set; }
    public string? ReplyToUserId { get; set; }
}
