using Common.Constants;
using Common.CurrentUserLogin;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Implement.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

        public string UserName =>
            IsAuthenticated
                ? (Principal!.Identity!.Name ?? CommonContants.SystemUser)
                : CommonContants.SystemUser;

        public string? Role => throw new NotImplementedException();

        public bool IsAdmin => throw new NotImplementedException();
    }
}
