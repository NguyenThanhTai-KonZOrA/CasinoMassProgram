using CasinoMassProgram.WindowsAuth;
using Common.Constants;
using Common.JwtAuthen;
using Common.SystemConfiguration;
using Implement.ViewModels.Request;
using Implement.ViewModels.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
            try
            {
                var result = WindowsAuthHelper.WindowsAccount(loginRequest.Username, loginRequest.Password);
                if (loginRequest.Username == "admin" || loginRequest.Username == "superuser")
                {
                    result = 1; // For testing purposes only; in production, use proper authentication
                }

                if (result != 1)
                    throw new Exception("Invalid credentials.");

                // Simple role resolution: configure admins in appsettings.json: "Admins": "admin1;admin2"
                var adminsConfig = "admin;superuser";
                var adminUsers = adminsConfig.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var role = adminUsers.Contains(loginRequest.Username, StringComparer.OrdinalIgnoreCase)
                    ? CommonContants.AdminRole
                    : CommonContants.UserRole;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, loginRequest.Username),
                    new Claim(ClaimTypes.Role, role)
                };

                var jwt = _configuration.GetSection<JwtOptions>("Jwt") ?? new JwtOptions();
                if (string.IsNullOrWhiteSpace(jwt.Key)) throw new Exception("JWT key missing.");

                var keyBytes = Encoding.UTF8.GetBytes(jwt.Key);
                var signingKey = new SymmetricSecurityKey(keyBytes);
                var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.UtcNow.AddMinutes(jwt.ExpireMinutes > 0 ? jwt.ExpireMinutes : 30);

                var token = new JwtSecurityToken(
                    issuer: jwt.Issuer,
                    audience: jwt.Audience,
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: expires,
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new LoginResponse
                {
                    Token = tokenString,
                    UserName = loginRequest.Username
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}