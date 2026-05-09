using Microsoft.EntityFrameworkCore;
using RuralTourism.Api.Entities;
using RuralTourism.Api.Enums;
using RuralTourism.Api.Migrations;
using RuralTourism.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace RuralTourism.Api.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<AppUser?> AuthenticateAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
        {
            var user= await _db.AppUsers
                .SingleOrDefaultAsync(u=>u.UserName==userNameOrEmail||u.Email==userNameOrEmail,cancellationToken);

            if (user == null)
            {
                // 尝试通过用户编号查找（将整型输入转换为整数）
                if (int.TryParse(userNameOrEmail, out int userNo))
                {
                    user = await _db.AppUsers
                        .SingleOrDefaultAsync(u => u.UserNo == userNo, cancellationToken);
                }
            }

            if (user == null) return null;
            
            //验证密码
            using var hmac=new HMACSHA512(user.PasswordSalt);
            var computed=hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            if (!CryptographicOperations.FixedTimeEquals(computed,user.PasswordHash))
            {
                return null;
            }

            if (user.BannedUntil.HasValue && user.BannedUntil.Value > DateTime.UtcNow)
            {
                return null;
            }

            return new AppUser
            {
                Id = user.Id,
                UserName = user.UserName,
                Nickname = user.Nickname,
                AvatarUrl = user.AvatarUrl,
                Email = user.Email,
                Role = user.Role,
                BannedUntil = user.BannedUntil,
                PasswordHash = user.PasswordHash,
                PasswordSalt = user.PasswordSalt,
                Following = user.Following,
                Followers = user.Followers
            };

        }

        public async Task<string> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            if (await _db.AppUsers.AnyAsync(u=>u.UserName==request.UserName,cancellationToken))
            {
                throw new Exception("用户名已经存在");
            }
            if (await _db.AppUsers.AnyAsync(u=>u.Email==request.Email,cancellationToken))
            {
                throw new Exception("邮箱已经存在");
            }


            using var hmac = new HMACSHA512();
            var salt= hmac.Key;
            var hash=hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Password));

            var user = new AppUser
            {
                UserName= request.UserName,
                Email= request.Email,
                PasswordHash= hash,
                PasswordSalt= salt,
                Role= UserRole.User
            };

            _db.AppUsers.Add(user);
            
            await _db.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}
