namespace RuralTourism.Api.DTOs;

public class UserInfoDto
{
    public string Id { get; set; } = null!;
    // 6?ùùùùID
    public string UserNo { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public string? AgeRange { get; set; }
    public string? HomeCity { get; set; }
    public string? TravelStyle { get; set; }
    public string? InterestTags { get; set; }
}

public class AdminUserDto
{
    public string Id { get; set; } = null!;
    public string UserNo { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? BannedUntil { get; set; }
    public bool IsBanned { get; set; }
    public bool IsPermanentBan { get; set; }
}

public class BanUserRequestDto
{
    public DateTimeOffset BanUntil { get; set; }
    public bool IsPermanent { get; set; }
}

public class UserSimpleDto
{
    public string Id { get; set; } = null!;
    public string UserNo { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class PublicUserProfileDto
{
    public string Id { get; set; } = null!;
    public string UserNo { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? HomeCity { get; set; }
    public string? InterestTags { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PublishedPostCount { get; set; }
    public DateTime? BannedUntil { get; set; }
    public bool IsBanned { get; set; }
    public bool IsPermanentBan { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsSelf { get; set; }
    public List<UserSimpleDto> FollowersPreview { get; set; } = [];
    public List<UserSimpleDto> FollowingPreview { get; set; } = [];
}

public class UserWallMessageDto
{
    public string Id { get; set; } = null!;
    public string SenderUserId { get; set; } = null!;
    public string SenderUserNo { get; set; } = null!;
    public string SenderDisplayName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserWallMessageCreateDto
{
    public string Content { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public string? AgeRange { get; set; }
    public string? HomeCity { get; set; }
    public string? TravelStyle { get; set; }
    public string? InterestTags { get; set; }
}

public class UpdateUserRoleDto
{
    public required string Role { get; set; }
}