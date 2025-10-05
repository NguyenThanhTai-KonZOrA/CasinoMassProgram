using System.Security.Claims;

namespace Common.CurrentUserLogin
{
    public interface ICurrentUserService
    {
        string UserName { get; }
        bool IsAuthenticated { get; }
        ClaimsPrincipal? Principal { get; }
    }
}
