using RuralTourism.Api.Entities;

namespace RuralTourism.Api.Services
{
    public interface ITokenService
    {
        ///<summary>
        /// 生成 JWT，返回 token 字符串与过期时间。
        /// </summary>
        (string Token, DateTime Expires) GenerateToken(AppUser user);
    }
}
