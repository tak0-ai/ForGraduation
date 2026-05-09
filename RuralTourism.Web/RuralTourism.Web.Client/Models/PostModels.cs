using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RuralTourism.Web.Client.Models;

public enum BlockType
{
    Text,
    Image,
    Video,
    Header,
    Header1,
    Header2,
    BoldText,
    UnorderedList,
    OrderedList
}

public sealed record PostBlockDto
(
    string Id,
    int Order,
    BlockType Type,
    string Content,
    string? Caption
);

public sealed record PostSummaryDto
(
    string Id,
    string? Title,
    string? CoverMediaId,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    PostStatus Status,
    string? RejectReason = null
);

public sealed record PostDetailDto
(
    string Id,
    string AuthorId,
    string? Title,
    string? CoverMediaId,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    PostStatus Status,
    List<PostBlockDto> Blocks,
    bool IsLiked,
    int LikeCount,
    bool IsCollected,
    int CollectCount,
    string? ReviewComment = null
);
public sealed record PostBlockCreateDto
(
    string? Id,
    int Order,
    BlockType Type,
    string Content,
    string? Caption
);

public sealed record PostCreateDto
(
    string? Title,
    bool IsDraft,
    string? CoverMediaId,
    List<PostBlockCreateDto> Blocks
);

public enum PostStatus
{
    Draft,
    Published,
    Archived,
    Private,
    PendingReview
}

public class CommentDto
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
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

public class NotificationDto
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Body { get; set; }
    public int Level { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PostId { get; set; }
    public string? TourPlanId { get; set; }
    public string? ChatRoomId { get; set; }

    public string? TriggerUserId { get; set; }
    public string? TriggerUserNo { get; set; }
    public string? TriggerUserName { get; set; }
    public string? TriggerUserAvatarUrl { get; set; }
}

