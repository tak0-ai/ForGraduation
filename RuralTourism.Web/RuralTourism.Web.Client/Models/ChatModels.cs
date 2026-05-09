namespace RuralTourism.Web.Client.Models;

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

    public string GetCoverUrl() => string.IsNullOrEmpty(CoverMediaId) ? UserConstants.DefaultAvatarUrl : CoverMediaId;
}

public class ChatRoomCreateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsGroup { get; set; }
    public string? TargetUserId { get; set; }
    public List<string>? MemberIds { get; set; }
}

public class ChatMemberDto
{
    public string UserId { get; set; } = null!;
    public string? UserNo { get; set; }
    public string UserName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public int Role { get; set; } // enum int
    public DateTime? MuteUntil { get; set; }
    public bool IsActive { get; set; }
}

public enum ChatMessageType
{
    Text,
    Image,
    Video,
    Location,
    Link
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

    public string GetAvatarUrl() => string.IsNullOrEmpty(AuthorAvatarUrl) ? UserConstants.DefaultAvatarUrl : AuthorAvatarUrl; 
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
    public string Type { get; set; } = null!; 
    public string? Description { get; set; }
    public bool IsJoined { get; set; }
    public string RelationStatus { get; set; } = "None"; // None, Pending, Inbound, Connected
    public int? NumberId { get; set; }

    public string GetAvatarUrl() => string.IsNullOrEmpty(AvatarUrl) ? UserConstants.DefaultAvatarUrl : AvatarUrl;
}

public class ChatRequestDto
{
    public string Id { get; set; } = null!;
    public string RequesterId { get; set; } = null!;
    public string RequesterName { get; set; } = null!;
    public string? RequesterAvatarUrl { get; set; }
    public ChatRequestType Type { get; set; }
    public string? TargetName { get; set; }
    public ChatRequestStatus Status { get; set; }
    public string? RequestMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    public string GetAvatarUrl() => string.IsNullOrEmpty(RequesterAvatarUrl) ? UserConstants.DefaultAvatarUrl : RequesterAvatarUrl;
}

public enum ChatRequestStatus
{
    Pending,
    Accepted,
    Rejected
}

public enum ChatRequestType
{
    Friend,
    GroupJoin,
    GroupInvite
}

