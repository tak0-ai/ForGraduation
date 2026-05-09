using RuralTourism.Api.Entities;
using RuralTourism.Api.Models;

namespace RuralTourism.Api.Services
{
    public interface IUserService
    {
        /// <summary>
        /// 注册用户，成功返回新用户 Id；若存在相同用户名或邮箱抛出 InvalidOperationException。
        /// </summary>
        Task<string> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证凭据。成功返回对应的 AppUser（不应在外层直接返回密码相关字段）；失败返回 null。
        /// </summary>
        Task<AppUser?> AuthenticateAsync(string userNameOrEmail,string password,CancellationToken cancellationToken = default);
    }
}
