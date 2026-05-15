using Microsoft.AspNetCore.Authorization;
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
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AdminUserDto>>> GetAdminUsers([FromQuery] string? keyword = null)
    {
        var now = DateTime.UtcNow;
        var query = _db.AppUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.Trim();
            query = query.Where(x => x.UserName.Contains(keyword) || x.Email.Contains(keyword) || (x.Nickname != null && x.Nickname.Contains(keyword)) || x.UserNo.ToString().Contains(keyword));
        }

        var items = await query
            .OrderByDescending(x => x.Role)
            .ThenByDescending(x => x.BannedUntil)
            .ThenBy(x => x.UserNo)
            .Select(x => new AdminUserDto
            {
                Id = x.Id,
                UserNo = x.UserNo.ToString("D6"),
                UserName = x.UserName,
                Email = x.Email,
                Nickname = x.Nickname,
                AvatarUrl = x.AvatarUrl,
                Role = x.Role.ToString(),
                BannedUntil = x.BannedUntil,
                IsBanned = x.BannedUntil.HasValue && x.BannedUntil.Value > now,
                IsPermanentBan = x.BannedUntil.HasValue && x.BannedUntil.Value.Year >= 9999
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("{userId}/ban")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BanUser(string userId, [FromBody] BanUserRequestDto dto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();
        if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase)) return BadRequest("?????????");

        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null) return NotFound();

        user.BannedUntil = dto.IsPermanent
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : dto.BanUntil.UtcDateTime;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{userId}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateUserRoleDto dto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();
        if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase)) return BadRequest("不能修改自己的权限");

        if (!Enum.TryParse<UserRole>(dto.Role, out var newRole))
        {
            return BadRequest("无效的用户角色");
        }

        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null) return NotFound();

        user.Role = newRole;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{userId}/ban")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnbanUser(string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();
        if (string.Equals(currentUserId, userId, StringComparison.OrdinalIgnoreCase)) return BadRequest("??????????");

        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null) return NotFound();

        user.BannedUntil = null;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("public/{userNo}")]
    public async Task<ActionResult<PublicUserProfileDto>> GetPublicProfile(string userNo)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();

        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (user == null) return NotFound();

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        var followers = await _db.Set<UserFollow>()
            .Where(f => f.FollowingId == user.Id)
            .Include(f => f.Follower)
            .OrderByDescending(f => f.CreatedAt)
            .Take(12)
            .ToListAsync();

        var following = await _db.Set<UserFollow>()
            .Where(f => f.FollowerId == user.Id)
            .Include(f => f.Following)
            .OrderByDescending(f => f.CreatedAt)
            .Take(12)
            .ToListAsync();

        var followersCount = await _db.Set<UserFollow>().CountAsync(f => f.FollowingId == user.Id);
        var followingCount = await _db.Set<UserFollow>().CountAsync(f => f.FollowerId == user.Id);
        var postCount = await _db.Posts.CountAsync(p => p.AuthorId == user.Id && p.Status == PostStatus.Published);
        var isBanned = user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow;

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isSelf = !string.IsNullOrWhiteSpace(currentUserId) && currentUserId == user.Id;
        var isFollowing = !string.IsNullOrWhiteSpace(currentUserId) && await _db.Set<UserFollow>()
            .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == user.Id);

        return Ok(new PublicUserProfileDto
        {
            Id = user.Id,
            UserNo = user.UserNo.ToString("D6"),
            UserName = user.UserName,
            DisplayName = user.Nickname ?? user.UserName,
            AvatarUrl = user.AvatarUrl,
            HomeCity = profile?.HomeCity,
            InterestTags = profile?.InterestTags,
            FollowersCount = followersCount,
            FollowingCount = followingCount,
            PublishedPostCount = postCount,
            BannedUntil = user.BannedUntil,
            IsBanned = isBanned,
            IsPermanentBan = user.BannedUntil.HasValue && user.BannedUntil.Value.Year >= 9999,
            IsFollowing = isFollowing,
            IsSelf = isSelf,
            FollowersPreview = followers
                .Where(f => f.Follower != null)
                .Select(f => new UserSimpleDto
                {
                    Id = f.Follower!.Id,
                    UserNo = f.Follower.UserNo.ToString("D6"),
                    DisplayName = f.Follower.Nickname ?? f.Follower.UserName,
                    AvatarUrl = f.Follower.AvatarUrl
                }).ToList(),
            FollowingPreview = following
                .Where(f => f.Following != null)
                .Select(f => new UserSimpleDto
                {
                    Id = f.Following!.Id,
                    UserNo = f.Following.UserNo.ToString("D6"),
                    DisplayName = f.Following.Nickname ?? f.Following.UserName,
                    AvatarUrl = f.Following.AvatarUrl
                }).ToList()
        });
    }

    [AllowAnonymous]
    [HttpGet("public/{userNo}/posts")]
    public async Task<IActionResult> GetPublicPosts(string userNo, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();
        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (user == null) return NotFound();

        var items = await _db.Posts
            .Where(p => p.AuthorId == user.Id && p.Status == PostStatus.Published)
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

    [AllowAnonymous]
    [HttpGet("public/{userNo}/following")]
    public async Task<ActionResult<List<UserSimpleDto>>> GetPublicFollowing(string userNo)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();
        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (user == null) return NotFound();

        var items = await _db.Set<UserFollow>()
            .Where(f => f.FollowerId == user.Id)
            .Include(f => f.Following)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new UserSimpleDto
            {
                Id = f.Following!.Id,
                UserNo = f.Following.UserNo.ToString("D6"),
                DisplayName = f.Following.Nickname ?? f.Following.UserName,
                AvatarUrl = f.Following.AvatarUrl
            })
            .ToListAsync();

        return Ok(items);
    }

    [AllowAnonymous]
    [HttpGet("public/{userNo}/followers")]
    public async Task<ActionResult<List<UserSimpleDto>>> GetPublicFollowers(string userNo)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();
        var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (user == null) return NotFound();

        var items = await _db.Set<UserFollow>()
            .Where(f => f.FollowingId == user.Id)
            .Include(f => f.Follower)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new UserSimpleDto
            {
                Id = f.Follower!.Id,
                UserNo = f.Follower.UserNo.ToString("D6"),
                DisplayName = f.Follower.Nickname ?? f.Follower.UserName,
                AvatarUrl = f.Follower.AvatarUrl
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("public/{userNo}/follow")]
    public async Task<IActionResult> ToggleFollow(string userNo)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

        var targetUser = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (targetUser == null) return NotFound();
        if (targetUser.Id == currentUserId) return BadRequest("?????????");

        var relation = await _db.Set<UserFollow>()
            .FirstOrDefaultAsync(x => x.FollowerId == currentUserId && x.FollowingId == targetUser.Id);

        if (relation == null)
        {
            _db.Set<UserFollow>().Add(new UserFollow
            {
                FollowerId = currentUserId,
                FollowingId = targetUser.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return Ok(new { isFollowing = true });
        }

        _db.Set<UserFollow>().Remove(relation);
        await _db.SaveChangesAsync();
        return Ok(new { isFollowing = false });
    }

    [AllowAnonymous]
    [HttpGet("public/{userNo}/messages")]
    public async Task<ActionResult<List<UserWallMessageDto>>> GetPublicWallMessages(string userNo)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();
        var targetUser = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (targetUser == null) return NotFound();

        var items = await _db.UserWallMessages
            .Where(x => x.TargetUserId == targetUser.Id)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new UserWallMessageDto
            {
                Id = x.Id,
                SenderUserId = x.SenderUserId,
                SenderUserNo = x.SenderUser != null ? x.SenderUser.UserNo.ToString("D6") : "",
                SenderDisplayName = x.SenderUser != null ? (x.SenderUser.Nickname ?? x.SenderUser.UserName) : "???",
                SenderAvatarUrl = x.SenderUser != null ? x.SenderUser.AvatarUrl : null,
                Content = x.Content,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("public/{userNo}/messages")]
    public async Task<ActionResult<UserWallMessageDto>> CreatePublicWallMessage(string userNo, [FromBody] UserWallMessageCreateDto dto)
    {
        if (!int.TryParse(userNo, out var parsedUserNo)) return NotFound();
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(currentUserId)) return Unauthorized();

        var targetUser = await _db.AppUsers.FirstOrDefaultAsync(x => x.UserNo == parsedUserNo);
        if (targetUser == null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest("??????????");

        var entity = new UserWallMessage
        {
            TargetUserId = targetUser.Id,
            SenderUserId = currentUserId,
            Content = dto.Content.Trim()
        };
        _db.UserWallMessages.Add(entity);
        await _db.SaveChangesAsync();

        var sender = await _db.AppUsers.FindAsync(currentUserId);
        return Ok(new UserWallMessageDto
        {
            Id = entity.Id,
            SenderUserId = currentUserId,
            SenderUserNo = sender?.UserNo.ToString("D6") ?? "",
            SenderDisplayName = sender?.Nickname ?? sender?.UserName ?? "???",
            SenderAvatarUrl = sender?.AvatarUrl,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt
        });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUserInfo()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null) return NotFound();

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        return Ok(new UserInfoDto
        {
            Id = user.Id,
            UserNo = user.UserNo.ToString("D6"),
            UserName = user.UserName,
            Email = user.Email,
            Nickname = user.Nickname,
            AvatarUrl = user.AvatarUrl,
            Gender = profile?.Gender,
            AgeRange = profile?.AgeRange,
            HomeCity = profile?.HomeCity,
            TravelStyle = profile?.TravelStyle,
            InterestTags = profile?.InterestTags
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUserInfo([FromBody] UpdateUserDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _db.AppUsers.FindAsync(userId);
        if (user == null) return NotFound();

        user.Nickname = dto.Nickname;
        user.AvatarUrl = dto.AvatarUrl;

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            _db.UserProfiles.Add(profile);
        }

        profile.Gender = dto.Gender;
        profile.AgeRange = dto.AgeRange;
        profile.HomeCity = dto.HomeCity;
        profile.TravelStyle = dto.TravelStyle;
        profile.InterestTags = dto.InterestTags;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("me/following")]
    public async Task<ActionResult<List<UserInfoDto>>> GetMyFollowings()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var followings = await _db.Set<UserFollow>()
            .Where(f => f.FollowerId == userId)
            .Include(f => f.Following)
            .Select(f => f.Following!)
            .ToListAsync();

        return Ok(followings.Select(u => new UserInfoDto
        {
            Id = u.Id,
            UserNo = u.UserNo.ToString("D6"),
            UserName = u.UserName,
            Email = u.Email,
            Nickname = u.Nickname,
            AvatarUrl = u.AvatarUrl,
            // Profile info might not be loaded, simplify for selection list
        }));
    }
}