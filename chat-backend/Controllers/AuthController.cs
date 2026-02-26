using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        // Mock login: acepta cualquier password no vacía
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username y password son requeridos");
        }

        var advisorId = request.Username.ToLowerInvariant();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, advisorId),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, "advisor")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return Ok(new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            advisorId,
            request.Username
        ));
    }
}
