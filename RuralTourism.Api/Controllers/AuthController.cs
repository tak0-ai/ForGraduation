using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Filters;
using RuralTourism.Api.Models;
using RuralTourism.Api.Migrations;
using RuralTourism.Api.Services;

namespace RuralTourism.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _users;
        private readonly ITokenService _token;
        private readonly ApplicationDbContext _db;

        public AuthController(IUserService users, ITokenService tokens, ApplicationDbContext db)
        {
            _users = users;
            _token = tokens;
            _db = db;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = await _users.RegisterAsync(request, cancellationToken);
                //返回201并指向一个假设的用户资源位置
                return Created($"/api/users/{userId}", new { id = userId });

            }
            catch (InvalidOperationException ex)
            {
                //唯一性冲突或业务错误
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message);
            }
        }

        [HttpPost("login")]
        [ServiceFilter(typeof(AuditLogFilter))]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _users.AuthenticateAsync(request.UserNameOrEmail, request.Password, cancellationToken);
            if (user==null)
            {
                var bannedUser = await FindUserByLoginAsync(request.UserNameOrEmail, cancellationToken);
                if (bannedUser?.BannedUntil.HasValue == true && bannedUser.BannedUntil.Value > DateTime.UtcNow)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = $"账号已被封禁至 {bannedUser.BannedUntil.Value.ToLocalTime():yyyy-MM-dd HH:mm}" });
                }

                return Unauthorized(new { error = "用户名或密码错误" });
            }
            var (token, expires) = _token.GenerateToken(user);
            return Ok(new { token, expires });
        }

        private async Task<RuralTourism.Api.Entities.AppUser?> FindUserByLoginAsync(string userNameOrEmail, CancellationToken cancellationToken)
        {
            var user = await _db.AppUsers.SingleOrDefaultAsync(u => u.UserName == userNameOrEmail || u.Email == userNameOrEmail, cancellationToken);
            if (user != null) return user;

            if (int.TryParse(userNameOrEmail, out var userNo))
            {
                user = await _db.AppUsers.SingleOrDefaultAsync(u => u.UserNo == userNo, cancellationToken);
            }

            return user;
        }
    }
}