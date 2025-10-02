using CasinoMassProgram.WindowsAuth;
using Common.SystemConfiguration;
using Implement.ViewModels.Request;
using Implement.ViewModels.Response;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoMassProgram.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISystemConfiguration _configuration;
        public AuthController(ISystemConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest loginRequest)
        {
            return Ok(new LoginResponse()
            {
                Token = "HardCode_Token",
                UserName = loginRequest.Username
            });
            try
            {
                var result = WindowsAuthHelper.WindowsAccount(loginRequest.Username, loginRequest.Password);
                if (result == 1)
                {
                    var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, loginRequest.Username)
                };

                    var expirationTime = 30;// _configuration.GetValue("ApplicationCookie:Expiration");
                    var cookieName = _configuration.GetValue("ApplicationCookie:CookieName");

                    var claimsIdentity = new ClaimsIdentity(claims, cookieName);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(expirationTime)
                    };

                    //await HttpContext.SignInAsync(cookieName, new ClaimsPrincipal(claimsIdentity), authProperties);
                }
                else if (result == 0)
                {
                    throw new Exception("Authentication failed.");
                }
                else if (result == -1)
                {
                    throw new Exception("The account is deactivated.");
                }
                else if (result == -2)
                {
                    throw new Exception("The username or password is incorrect.");
                }
                return Ok(new LoginResponse()
                {
                    Token = "HardCode_Token",
                    UserName = loginRequest.Username
                });
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
    }
}
