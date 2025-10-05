using Common.Constants;
using Microsoft.AspNetCore.Mvc;

namespace CasinoMassProgram.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected string CurrentUserName =>
            User?.Identity?.IsAuthenticated == true
                ? (User.Identity?.Name ?? CommonContants.SystemUser)
                : CommonContants.SystemUser;
    }
}