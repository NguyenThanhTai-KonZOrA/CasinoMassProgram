using System.Security.Claims;

namespace Common.CurrentUserLogin
{
    public interface ICurrentUserService
    {
        string UserName { get; }
        bool IsAuthenticated { get; }
        ClaimsPrincipal? Principal { get; }
        string? Role { get; }
        bool IsAdmin { get; }
    }
}
