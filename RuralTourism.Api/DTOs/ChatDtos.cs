using RuralTourism.Api.Enums;

namespace RuralTourism.Api.DTOs;

public class ChatRoomDto
{
    public string Id { get; set; } = null!;
    public int RoomNo { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsGroup { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CoverMediaId { get; set; }
    public int UnreadCount { get; set; }
    public ChatMessageDto? LastMessage { get; set; }
    public List<ChatMemberDto> Members { get; set; } = [];
}

public class ChatRoomCreateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsGroup { get; set; }
    public string? TargetUserId { get; set; } // for 1-on-1 chat
    public List<string>? MemberIds { get; set; } // for group chat
}

public class ChatMemberDto
{
    public string UserId { get; set; } = null!;
    public string? UserNo { get; set; }
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public ChatMemberRole Role { get; set; }
    public DateTime? MuteUntil { get; set; }
    public bool IsActive { get; set; }
}

public class ChatMessageDto
{
    public string Id { get; set; } = null!;
    public string ChatRoomId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    public string? AuthorUserNo { get; set; }
    public string AuthorName { get; set; } = null!;
    public string? AuthorAvatarUrl { get; set; }
    public ChatMessageType Type { get; set; }
    public string Content { get; set; } = null!;
    public DateTime SentAt { get; set; }
}

public class ChatMessageCreateDto
{
    public string ChatRoomId { get; set; } = null!;
    public ChatMessageType Type { get; set; }
    public string Content { get; set; } = null!;
}

public class SearchResultDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Type { get; set; } = null!; // "User" or "Group"
    public string? Description { get; set; }
    public bool IsJoined { get; set; } // Deprecated or kept for compat, prefer RelationStatus
    public string RelationStatus { get; set; } = "None"; // None, Pending, Connected
    public int? NumberId { get; set; } // UserNo or RoomNo
}

public class ChatRequestDto
{
    public string Id { get; set; } = null!;
    public string RequesterId { get; set; } = null!;
    public string RequesterName { get; set; } = null!;
    public string? RequesterAvatarUrl { get; set; }
    public ChatRequestType Type { get; set; }
    public string? TargetName { get; set; } // User Name or Group Name
    public ChatRequestStatus Status { get; set; }
    public string? RequestMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
