using CasinoMassProgram.WindowsAuth;
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
                if (result == 1)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, loginRequest.Username)
                    };

                    // Read JWT options from configuration
                    var jwt = _configuration.GetSection<JwtOptions>("Jwt") ?? new JwtOptions();
                    if (string.IsNullOrWhiteSpace(jwt.Key))
                        throw new Exception("JWT signing key is not configured (Jwt:Key).");

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

                // Fallback (should not reach here)
                throw new Exception("Unknown authentication result.");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}