using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.DTOs;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Enums;
using RuralTourism.Api.Hubs;
using RuralTourism.Api.Migrations;
using System.Security.Claims;

namespace RuralTourism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(ApplicationDbContext db, IHubContext<ChatHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    // 获取我的聊天室列表
    [HttpGet("rooms")]
    public async Task<ActionResult<List<ChatRoomDto>>> GetMyRooms()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Include archived rooms where user was a member
        // AND include 1-on-1 rooms even if user is inactive (deleted friend)
        var memberOf = await _db.Set<ChatMember>()
            .Where(m => m.UserId == userId && (m.IsActive || m.ChatRoom!.IsArchived || !m.ChatRoom.IsGroup))
            .Include(m => m.ChatRoom)
                .ThenInclude(r => r!.Members)
                    .ThenInclude(m => m.User)
            .Include(m => m.ChatRoom)
                .ThenInclude(r => r!.Messages.OrderByDescending(msg => msg.SentAt).Take(1))
            .ToListAsync();

        var dtos = new List<ChatRoomDto>();
        foreach (var m in memberOf)
        {
            var r = m.ChatRoom;
            if (r == null) continue;

            // If it's a group, and not archived, and user is inactive => User left group. Should NOT show.
            if (r.IsGroup && !r.IsArchived && !m.IsActive) continue;

            string roomName = r.Name;
            string? cover = r.CoverMediaId;

            // 对于私聊（1对1），使用对方的名称/头像
            if (!r.IsGroup)
            {
                var other = r.Members.FirstOrDefault(x => x.UserId != userId)?.User;
                if (other != null)
                {
                    roomName = other.Nickname ?? other.UserName;
                    cover = other.AvatarUrl;
                }
            }

            var lastMsg = r.Messages.FirstOrDefault();
            
            // Sort Members: Owner(0) > Admin(1) > Member(2). Then Name.
            var sortedMembers = r.Members
                .OrderBy(mem => mem.Role)
                .ThenBy(mem => mem.User?.Nickname ?? mem.User?.UserName ?? "")
                .Select(m => new ChatMemberDto
                {
                    UserId = m.UserId,
                    UserNo = m.User != null ? m.User.UserNo.ToString("D6") : null,
                    UserName = m.User?.Nickname ?? m.User?.UserName ?? "Unknown",
                    AvatarUrl = m.User?.AvatarUrl,
                    Role = m.Role,
                    MuteUntil = m.MuteUntil,
                    IsActive = m.IsActive
                }).ToList();

            dtos.Add(new ChatRoomDto
            {
                Id = r.Id,
                RoomNo = r.RoomNo,
                Name = roomName,
                IsGroup = r.IsGroup,
                IsArchived = r.IsArchived,
                CoverMediaId = cover,
                CreatedAt = r.CreatedAt,
                Members = sortedMembers,
                LastMessage = lastMsg != null ? new ChatMessageDto
                {
                    Id = lastMsg.Id,
                    Content = lastMsg.Type == ChatMessageType.Text ? lastMsg.Content : $"[{lastMsg.Type}]",
                    SentAt = lastMsg.SentAt,
                    AuthorName = "" // 在列表视图中简化处理
                } : null
            });
        }

        return Ok(dtos.OrderByDescending(x => x.LastMessage?.SentAt ?? x.CreatedAt));
    }

    // 创建聊天室（1对1或群组）
    [HttpPost("rooms")]
    public async Task<ActionResult<ChatRoomDto>> CreateRoom([FromBody] ChatRoomCreateDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        ChatRoom room;

        if (!dto.IsGroup)
        {
            // 1对1聊天：检查是否已存在
            if (string.IsNullOrEmpty(dto.TargetUserId)) return BadRequest("Target user required for 1-on-1");
            
            // 检查已存在的唯一1对1房间
            // 简单逻辑：找到两人都是成员且非群组的房间
            var existing = await _db.ChatRooms
                .Where(r => !r.IsGroup && r.Members.Any(m => m.UserId == userId) && r.Members.Any(m => m.UserId == dto.TargetUserId))
                .FirstOrDefaultAsync();
            
            if (existing != null)
            {
                return Ok(new { id = existing.Id }); // 返回现有ID
            }

            // 不允许直接创建新的1对1聊天，必须使用请求/接受流程
            return BadRequest("Please send a friend request first.");
        }
        else
        {
            // Group
            room = new ChatRoom
            {
                Name = dto.Name ?? "New Group",
                IsGroup = true,
                CreatedById = userId,
                Description = dto.Description,
                RoomNo = new Random().Next(1000000, 9999999) // Add RoomNo
            };
            
            room.Members.Add(new ChatMember { ChatRoomId = room.Id, UserId = userId, Role = ChatMemberRole.Owner });
            
            if (dto.MemberIds != null)
            {
                foreach (var mid in dto.MemberIds.Where(id => id != userId).Distinct())
                {
                    room.Members.Add(new ChatMember { ChatRoomId = room.Id, UserId = mid, Role = ChatMemberRole.Member });
                }
            }
        }

        _db.ChatRooms.Add(room);
        await _db.SaveChangesAsync();

        // 如果是群组，在返回前加载成员信息以便前端可以直接使用
        if (room.IsGroup)
        {
             await _db.Entry(room).Collection(r => r.Members).LoadAsync();
             foreach(var m in room.Members)
             {
                  await _db.Entry(m).Reference(x => x.User).LoadAsync();
             }
             
             return Ok(new ChatRoomDto
            {
                Id = room.Id,
                RoomNo = room.RoomNo,
                Name = room.Name,
                IsGroup = true,
                CoverMediaId = room.CoverMediaId,
                CreatedAt = room.CreatedAt,
                Members = room.Members.Select(m => new ChatMemberDto 
                {
                    UserId = m.UserId,
                     UserNo = m.User != null ? m.User.UserNo.ToString("D6") : null,
                    UserName = m.User?.Nickname ?? m.User?.UserName ?? "Unknown",
                    AvatarUrl = m.User?.AvatarUrl,
                    Role = m.Role,
                    MuteUntil = m.MuteUntil
                }).ToList()
            });
        }

        return Ok(new { id = room.Id });
    }

    // 获取消息记录
    [HttpGet("rooms/{roomId}/messages")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetMessages(string roomId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // 验证成员身份
        var member = await _db.Set<ChatMember>().AnyAsync(m => m.ChatRoomId == roomId && m.UserId == userId);
        if (!member) return Forbid();

        var msgs = await _db.Set<ChatMessage>()
            .Where(m => m.ChatRoomId == roomId)
            .Include(m => m.Author)
            .OrderBy(m => m.SentAt) // 目前加载全部，稍后可按需分页
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ChatRoomId = m.ChatRoomId,
                AuthorId = m.AuthorId,
                AuthorUserNo = m.Author != null ? m.Author.UserNo.ToString("D6") : null,
                AuthorName = m.Author != null ? (m.Author.Nickname ?? m.Author.UserName) : "Unknown",
                AuthorAvatarUrl = m.Author != null ? m.Author.AvatarUrl : null,
                Content = m.Content,
                Type = m.Type,
                SentAt = m.SentAt
            })
            .ToListAsync();

        return Ok(msgs);
    }

    // 发送消息
    [HttpPost("messages")]
    public async Task<ActionResult<ChatMessageDto>> SendMessage([FromBody] ChatMessageCreateDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // 验证成员身份
        var member = await _db.Set<ChatMember>()
            .FirstOrDefaultAsync(m => m.ChatRoomId == dto.ChatRoomId && m.UserId == userId);
        
        if (member == null || !member.IsActive) return Forbid();

        // 检查归档状态
        var room = await _db.ChatRooms.FindAsync(dto.ChatRoomId);
        if (room != null && room.IsArchived) return BadRequest("Room is archived");
        
        // 检查禁言
        if (member.MuteUntil.HasValue && member.MuteUntil.Value > DateTime.UtcNow)
        {
            return BadRequest($"You are muted until {member.MuteUntil.Value.ToLocalTime()}");
        }

        var msg = new ChatMessage
        {
            ChatRoomId = dto.ChatRoomId,
            AuthorId = userId!,
            Content = dto.Content,
            Type = dto.Type,
            SentAt = DateTime.UtcNow
        };

        _db.Set<ChatMessage>().Add(msg);
        await _db.SaveChangesAsync();

        // 加载作者信息用于DTO
        var author = await _db.AppUsers.FindAsync(userId);

        var result = new ChatMessageDto
        {
            Id = msg.Id,
            ChatRoomId = msg.ChatRoomId,
            AuthorId = msg.AuthorId,
            AuthorUserNo = author?.UserNo.ToString("D6"),
            AuthorName = author?.Nickname ?? author?.UserName ?? "Unknown",
            AuthorAvatarUrl = author?.AvatarUrl,
            Content = msg.Content,
            Type = msg.Type,
            SentAt = msg.SentAt
        };

        // 通过 SignalR 实时推送
        // 通知房间内的所有人
        await _hubContext.Clients.Group($"Room_{dto.ChatRoomId}").SendAsync("ReceiveMessage", result);

        // 给其他成员发送持久化的消息通知 (群聊/私聊)
        if (room != null)
        {
            var otherMembers = await _db.Set<ChatMember>()
                .Where(m => m.ChatRoomId == dto.ChatRoomId && m.UserId != userId && m.IsActive)
                .ToListAsync();

            var notificationBody = BuildGroupMessageNotificationBody(result.AuthorName, result.Type, result.Content, room.IsGroup);
            foreach (var m in otherMembers)
            {
                var notification = new Notification
                {
                    UserId = m.UserId,
                    Title = room.IsGroup ? "群聊消息" : "私聊消息",
                    Body = notificationBody,
                    Level = NotificationLevel.Info,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    ChatRoomId = room.Id,
                    TriggerUserId = userId
                };
                _db.Notifications.Add(notification);
            }
            await _db.SaveChangesAsync();
        }

        return Ok(result);
    }

    private static string BuildGroupMessageNotificationBody(string? authorName, ChatMessageType type, string? content, bool isGroup = true)
    {
        var sender = string.IsNullOrWhiteSpace(authorName) ? (isGroup ? "有成员" : "对方") : authorName;
        var text = type switch
        {
            ChatMessageType.Image => "[图片]",
            ChatMessageType.Video => "[视频]",
            ChatMessageType.Location => "[位置]",
            ChatMessageType.Link => "[链接]",
            _ => content ?? string.Empty
        };

        var brief = string.IsNullOrWhiteSpace(text)
            ? "发来一条新消息"
            : (text.Length <= 60 ? text : text[..60] + "...");

        return $"{sender}: {brief}";
    }

    [HttpDelete("friends/{targetUserId}")]
    public async Task<IActionResult> DeleteFriend(string targetUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // 查找1对1房间
        var room = await _db.ChatRooms
            .Include(r => r.Members)
            .Where(r => !r.IsGroup && r.Members.Any(m => m.UserId == userId) && r.Members.Any(m => m.UserId == targetUserId))
            .FirstOrDefaultAsync();

        if (room == null) return NotFound("Friendship not found");

        var member = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null)
        {
            member.IsActive = false;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("rooms/{roomId}/mute")]
    public async Task<IActionResult> MuteMember(string roomId, [FromBody] MuteRequestDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var roomStart = await _db.ChatRooms.Where(r => r.Id == roomId).Select(r => new { r.IsGroup }).FirstOrDefaultAsync();
        if (roomStart == null) return NotFound();
        if (!roomStart.IsGroup) return BadRequest("Only for groups");

        // 权限检查
        var members = await _db.Set<ChatMember>().Where(m => m.ChatRoomId == roomId).ToListAsync();
        var operatorMember = members.FirstOrDefault(m => m.UserId == userId);
        var targetMember = members.FirstOrDefault(m => m.UserId == dto.TargetUserId);

        if (operatorMember == null || targetMember == null) return NotFound("Member not found");
        if (userId == dto.TargetUserId) return BadRequest("Cannot mute self");

        // 权限等级: Owner > Admin > Member
        if (operatorMember.Role == ChatMemberRole.Member) return Forbid();
        if (operatorMember.Role == ChatMemberRole.Admin && (targetMember.Role == ChatMemberRole.Owner || targetMember.Role == ChatMemberRole.Admin)) return Forbid();
        
        targetMember.MuteUntil = DateTime.UtcNow.AddMinutes(dto.Minutes);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Muted" });
    }

    [HttpPost("rooms/{roomId}/kick")]
    public async Task<IActionResult> KickMember(string roomId, [FromBody] string targetUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var roomStart = await _db.ChatRooms.Where(r => r.Id == roomId).Select(r => new { r.IsGroup }).FirstOrDefaultAsync();
        if (roomStart == null) return NotFound();
        if (!roomStart.IsGroup) return BadRequest("Only for groups");

        var members = await _db.Set<ChatMember>().Where(m => m.ChatRoomId == roomId).ToListAsync();
        var operatorMember = members.FirstOrDefault(m => m.UserId == userId);
        var targetMember = members.FirstOrDefault(m => m.UserId == targetUserId);

        if (operatorMember == null || targetMember == null) return NotFound("Member not found");
        if (userId == targetUserId) return BadRequest("Cannot kick self");

        if (operatorMember.Role == ChatMemberRole.Member) return Forbid();
        if (operatorMember.Role == ChatMemberRole.Admin && (targetMember.Role == ChatMemberRole.Owner || targetMember.Role == ChatMemberRole.Admin)) return Forbid();

        _db.Set<ChatMember>().Remove(targetMember);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Kicked" });
    }

    public class MuteRequestDto
    {
        public string TargetUserId { get; set; } = null!;
        public int Minutes { get; set; }
    }

    [HttpPost("groups/{groupId}/leave")]
    public async Task<IActionResult> LeaveGroup(string groupId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var member = await _db.Set<ChatMember>()
            .FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == userId);
        
        if (member == null || !member.IsActive) return BadRequest("Not a member");

        // Owner cannot leave directly (must transfer or dismiss), unless specified otherwise.
        // Requirement implies "Member can exit". "Admin can exit".
        // If owner tries to leave, we might block or require dismiss.
        // Assuming Owner uses "Dismiss" instead.
        if (member.Role == ChatMemberRole.Owner) return BadRequest("Owner cannot leave. Use Dismiss.");

        member.IsActive = false;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Left group" });
    }

    [HttpPost("groups/{groupId}/dismiss")]
    public async Task<IActionResult> DismissGroup(string groupId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _db.ChatRooms.FindAsync(groupId);
        if (room == null || !room.IsGroup) return NotFound();

        var member = await _db.Set<ChatMember>()
            .FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == userId);
        
        if (member == null || member.Role != ChatMemberRole.Owner) return Forbid("Only owner dissmiss");

        room.IsArchived = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Group dismissed" });
    }

    [HttpPost("groups/{groupId}/rename")]
    public async Task<IActionResult> RenameGroup(string groupId, [FromBody] string newName)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(newName)) return BadRequest("Name required");

        var room = await _db.ChatRooms.FindAsync(groupId);
        if (room == null || !room.IsGroup) return NotFound();

        var member = await _db.Set<ChatMember>()
            .FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == userId);
        
        // Admin or Owner
        if (member == null || member.Role == ChatMemberRole.Member) return Forbid();

        room.Name = newName;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Renamed" });
    }

    [HttpPost("groups/{groupId}/invite")]
    public async Task<IActionResult> InviteToGroup(string groupId, [FromBody] string targetUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var roomStart = await _db.ChatRooms.Where(r => r.Id == groupId).Select(r => new { r.IsGroup }).FirstOrDefaultAsync();
        if (roomStart == null) return NotFound();
        if (!roomStart.IsGroup) return BadRequest("Only for groups");

        var myself = await _db.Set<ChatMember>().FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == userId);
        if (myself == null || !myself.IsActive) return Forbid();

        // Check if target is already in group
        var exists = await _db.Set<ChatMember>().AnyAsync(m => m.ChatRoomId == groupId && m.UserId == targetUserId && m.IsActive);
        if (exists) return Ok(new { message = "Already member" });

        // Logic:
        // If I am Owner or Admin -> Direct Add
        // If I am Member -> Create Request (Requester=Me, TargetUser=Friend, TargetGroup=Group, Type=GroupInvite)

        if (myself.Role == ChatMemberRole.Owner || myself.Role == ChatMemberRole.Admin)
        {
            // Revive inactive or add new
            var existingMember = await _db.Set<ChatMember>().FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == targetUserId);
            if (existingMember != null)
            {
                existingMember.IsActive = true;
                existingMember.Role = ChatMemberRole.Member; // Reset role
            }
            else
            {
                _db.Set<ChatMember>().Add(new ChatMember { ChatRoomId = groupId, UserId = targetUserId, Role = ChatMemberRole.Member });
            }
            await _db.SaveChangesAsync();
            return Ok(new { message = "Added" });
        }
        else
        {
            // Check pending requests
            var pending = await _db.ChatRequests.AnyAsync(r => 
                r.Type == ChatRequestType.GroupInvite && 
                r.TargetGroupId == groupId && 
                r.TargetUserId == targetUserId && 
                r.Status == ChatRequestStatus.Pending);
            
            if (pending) return Ok(new { message = "Invite pending" });

            var req = new ChatRequest
            {
                RequesterId = userId,
                TargetGroupId = groupId, // Group
                TargetUserId = targetUserId, // Who is being invited
                Type = ChatRequestType.GroupInvite,
                Status = ChatRequestStatus.Pending
            };
            _db.ChatRequests.Add(req);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Invite sent" });
        }
    }

    [HttpPost("groups/{groupId}/role")]
    public async Task<IActionResult> SetGroupRole(string groupId, [FromBody] SetRoleDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Only Owner can change roles
        var myself = await _db.Set<ChatMember>().FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == userId);
        if (myself == null || myself.Role != ChatMemberRole.Owner) return Forbid();

        var target = await _db.Set<ChatMember>().FirstOrDefaultAsync(m => m.ChatRoomId == groupId && m.UserId == dto.TargetUserId);
        if (target == null) return NotFound("Member not found");

        if (target.Role == ChatMemberRole.Owner) return BadRequest("Cannot change owner role here");

        // Toggle Admin/Member
        if (dto.IsAdmin) target.Role = ChatMemberRole.Admin;
        else target.Role = ChatMemberRole.Member;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Role updated" });
    }

    public class SetRoleDto
    {
        public string TargetUserId { get; set; } = null!;
        public bool IsAdmin { get; set; }
    }


    // 统一搜索接口
    [HttpGet("search")]
    public async Task<ActionResult<List<SearchResultDto>>> Search([FromQuery] string query, [FromQuery] string type = "All")
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

        var results = new List<SearchResultDto>();
        bool searchUsers = type == "All" || type == "User";
        bool searchGroups = type == "All" || type == "Group";
        
        // 解析数字查询以进行精确ID搜索
        bool isNumeric = int.TryParse(query, out int numQuery);

        if (searchUsers)
        {
            // 排除当前用户
            var userQuery = _db.AppUsers.Where(u => u.Id != currentUserId).AsQueryable();

            if (isNumeric)
            {
                // 如果是数字，既匹配UserNo，也匹配用户名或昵称包含该数字字符串
                userQuery = userQuery.Where(u => u.UserNo == numQuery 
                                              || u.UserName.Contains(query) 
                                              || (u.Nickname != null && u.Nickname.Contains(query)));
            }
            else
            {
                userQuery = userQuery.Where(u => u.UserName.Contains(query) || (u.Nickname != null && u.Nickname.Contains(query)));
            }

            var users = await userQuery.Take(20).ToListAsync();

            // 检查关系状态
            // 1. 现有的1对1房间
            var myFriendIds = await _db.ChatRooms
                .Where(r => !r.IsGroup && r.Members.Any(m => m.UserId == currentUserId))
                .SelectMany(r => r.Members)
                .Where(m => m.UserId != currentUserId)
                .Select(m => m.UserId)
                .ToListAsync();

            // 2. 待处理的请求
            var myRequests = await _db.ChatRequests
                .Where(r => r.Status == ChatRequestStatus.Pending && 
                           (r.RequesterId == currentUserId || r.TargetUserId == currentUserId) &&
                           r.Type == ChatRequestType.Friend)
                .ToListAsync();

            results.AddRange(users.Select(u => {
                string status = "None";
                if (myFriendIds.Contains(u.Id)) 
                {
                    status = "Connected";
                }
                else 
                {
                    var req = myRequests.FirstOrDefault(r => r.RequesterId == u.Id || r.TargetUserId == u.Id);
                    if (req != null)
                    {
                        if (req.RequesterId == currentUserId) status = "Pending";
                        else status = "Inbound";
                    }
                }

                return new SearchResultDto
                {
                    Id = u.Id,
                    NumberId = u.UserNo,
                    Name = u.Nickname ?? u.UserName,
                    AvatarUrl = u.AvatarUrl,
                    Type = "User",
                    Description = null, // don't show email
                    IsJoined = status == "Connected",
                    RelationStatus = status
                };
            }));
        }

        if (searchGroups)
        {
            var groupQuery = _db.ChatRooms.Where(r => r.IsGroup).AsQueryable();
            if (isNumeric)
            {
                // 如果是数字，既匹配RoomNo，也匹配群名包含该数字字符串
                groupQuery = groupQuery.Where(r => r.RoomNo == numQuery || r.Name.Contains(query));
            }
            else
            {
                groupQuery = groupQuery.Where(r => r.Name.Contains(query));
            }

            var groups = await groupQuery.Take(20).ToListAsync();
            
            // 检查群成员身份
            var myGroupIds = await _db.ChatRooms
                .Where(r => r.IsGroup && r.Members.Any(m => m.UserId == currentUserId))
                .Select(r => r.Id)
                .ToListAsync();

            // 检查待处理的加入请求
            var myJoinRequests = await _db.ChatRequests
                .Where(r => r.Status == ChatRequestStatus.Pending && r.RequesterId == currentUserId && r.Type == ChatRequestType.GroupJoin)
                .Select(r => r.TargetGroupId)
                .ToListAsync();

            results.AddRange(groups.Select(g => {
                string status = "None";
                if (myGroupIds.Contains(g.Id)) status = "Connected";
                else if (myJoinRequests.Contains(g.Id)) status = "Pending";

                return new SearchResultDto
                {
                    Id = g.Id,
                    NumberId = g.RoomNo,
                    Name = g.Name,
                    AvatarUrl = g.CoverMediaId,
                    Type = "Group",
                    Description = g.Description,
                    IsJoined = status == "Connected",
                    RelationStatus = status
                };
            }));
        }

        return Ok(results);
    }

    [HttpPost("groups/{groupId}/join")]
    public async Task<IActionResult> JoinGroup(string groupId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var room = await _db.ChatRooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == groupId);
        if (room == null) return NotFound("Group not found");
        if (!room.IsGroup) return BadRequest("Cannot join 1-on-1 chat via this method");

        if (room.Members.Any(m => m.UserId == userId))
        {
            return Ok(new { message = "Already a member" });
        }

        // 检查是否存在待处理请求
        var existingReq = await _db.ChatRequests.AnyAsync(r => 
            r.RequesterId == userId && 
            r.TargetGroupId == groupId && 
            r.Status == ChatRequestStatus.Pending);
            
        if (existingReq) return Ok(new { message = "Request already pending" });

        // 创建请求
        var req = new ChatRequest
        {
            RequesterId = userId,
            TargetGroupId = groupId,
            Type = ChatRequestType.GroupJoin,
            Status = ChatRequestStatus.Pending
        };
        _db.ChatRequests.Add(req);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Request sent" });
    }

    [HttpGet("requests")]
    public async Task<ActionResult<List<ChatRequestDto>>> GetRequests()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // 1. 发给我的好友请求
        // 2. 发给我是群主/管理员的群组加入请求
        
        // 查找我拥有的群组
        var myGroupIds = await _db.Set<ChatMember>()
            .Where(m => m.UserId == userId && (m.Role == ChatMemberRole.Owner || m.Role == ChatMemberRole.Admin) && m.ChatRoom != null && m.ChatRoom.IsGroup)
            .Select(m => m.ChatRoomId)
            .ToListAsync();

        var requests = await _db.ChatRequests
            .Include(r => r.Requester)
            .Include(r => r.TargetUser)
            .Include(r => r.TargetGroup)
            .Where(r => 
                (r.Type == ChatRequestType.Friend && r.TargetUserId == userId) ||
                ( (r.Type == ChatRequestType.GroupJoin || r.Type == ChatRequestType.GroupInvite) && myGroupIds.Contains(r.TargetGroupId!))
            )
            .Where(r => r.Status == ChatRequestStatus.Pending) // 仅待处理
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(requests.Select(r => new ChatRequestDto
        {
            Id = r.Id,
            RequesterId = r.RequesterId,
            RequesterName = r.Requester?.Nickname ?? r.Requester?.UserName ?? "Unknown",
            RequesterAvatarUrl = r.Requester?.AvatarUrl,
            Type = r.Type,
            TargetName = r.Type == ChatRequestType.Friend ? (r.TargetUser?.Nickname) : 
                         r.Type == ChatRequestType.GroupInvite ? $"{r.TargetGroup?.Name} (Invite {r.TargetUser?.Nickname})" : // Show who is being invited
                         r.TargetGroup?.Name,
            Status = r.Status,
            RequestMessage = r.RequestMessage,
            CreatedAt = r.CreatedAt
        }));
    }

    [HttpPost("requests/send")]
    public async Task<IActionResult> SendRequest([FromBody] ChatRequestDto dto)
    {
         var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (dto.Type == ChatRequestType.Friend)
        {
            if (string.IsNullOrEmpty(dto.RequesterId)) return BadRequest("Target User ID needed (passed in RequesterId for compatibility or use generic dto)");
            // 注意：dto.RequesterId 通常意味着“谁调用的”，但在这里我们可能使用 DTO 来携带目标对象？
            // 假设输入的 DTO 使用 'TargetUserId' 字段... 等等，DTO 没有明确命名它。
            // 让我们依赖查询参数或干净的 DTO。
        }
        return BadRequest("Use specific endpoints");
    }
    
    // 更好的简化发送端点
    [HttpPost("requests/friend/{targetUserId}")]
    public async Task<IActionResult> SendFriendRequest(string targetUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        if (userId == targetUserId) return BadRequest("Cannot add self");

        // 检查是否已经是好友
        var existingRoom = await _db.ChatRooms
            .Where(r => !r.IsGroup && r.Members.Any(m => m.UserId == userId) && r.Members.Any(m => m.UserId == targetUserId))
            .AnyAsync();
        if (existingRoom) return Ok(new { message = "Already friends" });

        // 检查是否待处理
        var pending = await _db.ChatRequests
            .AnyAsync(r => r.Type == ChatRequestType.Friend && 
                ((r.RequesterId == userId && r.TargetUserId == targetUserId) || 
                 (r.RequesterId == targetUserId && r.TargetUserId == userId)) &&
                 r.Status == ChatRequestStatus.Pending);
        
        if (pending) return Ok(new { message = "Request pending" });

        var req = new ChatRequest
        {
            RequesterId = userId,
            TargetUserId = targetUserId,
            Type = ChatRequestType.Friend
        };
        _db.ChatRequests.Add(req);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Request sent" });
    }

    [HttpPost("requests/{requestId}/handle/{status}")] // status: accept or reject
    public async Task<IActionResult> HandleRequest([FromRoute] string requestId, [FromRoute] string status)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var req = await _db.ChatRequests
            .Include(r => r.TargetGroup)
            .FirstOrDefaultAsync(r => r.Id == requestId);
            
        if (req == null) return NotFound();
        // 如果状态已经是已处理，直接返回成功
        if (req.Status != ChatRequestStatus.Pending) return Ok(new { message = "Already handled" });

        // 验证权限
        if (req.Type == ChatRequestType.Friend)
        {
            if (req.TargetUserId != userId) return Forbid();
        }
        else if (req.Type == ChatRequestType.GroupJoin || req.Type == ChatRequestType.GroupInvite)
        {
            // 必须是群主或管理员
            var isAuth = await _db.Set<ChatMember>().AnyAsync(m => m.ChatRoomId == req.TargetGroupId && m.UserId == userId && (m.Role == ChatMemberRole.Owner || m.Role == ChatMemberRole.Admin));
            if (!isAuth) return Forbid();
        }

        if (status.ToLower() == "reject")
        {
            req.Status = ChatRequestStatus.Rejected;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Rejected" });
        }
        
        if (status.ToLower() == "accept")
        {
            req.Status = ChatRequestStatus.Accepted;
            
            // 执行业务逻辑
            if (req.Type == ChatRequestType.Friend)
            {
                // 检查是否已存在房间（避免重复创建）
                var exists = await _db.ChatRooms.AnyAsync(r => !r.IsGroup && r.Members.Any(m => m.UserId == req.RequesterId) && r.Members.Any(m => m.UserId == req.TargetUserId));
                
                if (!exists)
                {
                    // 创建房间
                    var room = new ChatRoom
                    {
                        Name = "Chat", 
                        IsGroup = false,
                        CreatedById = req.RequesterId,
                        // 手动生成 RoomNo 以避免唯一索引冲突 (SQLite 默认 int 为 0)
                        RoomNo = new Random().Next(1000000, 9999999) 
                    };
                    room.Members.Add(new ChatMember { ChatRoomId = room.Id, UserId = req.RequesterId, Role = ChatMemberRole.Owner });
                    room.Members.Add(new ChatMember { ChatRoomId = room.Id, UserId = req.TargetUserId!, Role = ChatMemberRole.Member });
                    _db.ChatRooms.Add(room);
                }
            }
            else if (req.Type == ChatRequestType.GroupJoin)
            {
                // 添加成员 (确保不重复)
                // GroupJoin: Requester wants to join
                var exists = await _db.Set<ChatMember>().AnyAsync(m => m.ChatRoomId == req.TargetGroupId && m.UserId == req.RequesterId);
                if (!exists)
                {
                     _db.Set<ChatMember>().Add(new ChatMember { ChatRoomId = req.TargetGroupId!, UserId = req.RequesterId, Role = ChatMemberRole.Member });
                }
            }
            else if (req.Type == ChatRequestType.GroupInvite)
            {
                // GroupInvite: Requester (member) invited TargetUser
                // Add TargetUser
                var exists = await _db.Set<ChatMember>().AnyAsync(m => m.ChatRoomId == req.TargetGroupId && m.UserId == req.TargetUserId);
                if (!exists)
                {
                    // Revive if needed?
                    var existing = await _db.Set<ChatMember>().FirstOrDefaultAsync(m => m.ChatRoomId == req.TargetGroupId && m.UserId == req.TargetUserId);
                    if (existing != null)
                    {
                        existing.IsActive = true;
                    }
                    else
                    {
                        _db.Set<ChatMember>().Add(new ChatMember { ChatRoomId = req.TargetGroupId!, UserId = req.TargetUserId!, Role = ChatMemberRole.Member });
                    }
                }
            }
            
            await _db.SaveChangesAsync();
            return Ok(new { message = "Accepted" });
        }

        return BadRequest("Invalid action");
    }

    // 获取我的好友列表（存在1对1聊天记录的用户）
    [HttpGet("friends")]
    public async Task<ActionResult<List<UserInfoDto>>> GetMyFriends()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var friends = await _db.ChatRooms
            .Where(r => !r.IsGroup && r.Members.Any(m => m.UserId == userId))
            .SelectMany(r => r.Members)
            .Where(m => m.UserId != userId)
            .Include(m => m.User)
            .Select(m => m.User)
            .Distinct()
            .ToListAsync();

        return Ok(friends.Select(u => new UserInfoDto
        {
            Id = u.Id,
            UserNo = u.UserNo != 0 ? u.UserNo.ToString("D6") : "000000",
            UserName = u.UserName,
            Email = u.Email,
            Nickname = u.Nickname,
            AvatarUrl = u.AvatarUrl,
        }));
    }
}
