using Applicate.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Applicate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        // 1. Tjek om bruger findes
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username && u.Password == req.Password);
        if (user == null) return Unauthorized("Forkert brugernavn eller kode");

        // 2. Opret Claims (Det der står på armbåndet)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Tilføj roller
        foreach (var role in user.Roles.Split(','))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        // 3. Generer Token (Underskriv armbåndet)
        // Nøglen skal matche den i Program.cs!
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "super_hemmelig_nøgle_der_er_mindst_32_tegn_lang"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "Applicate",
            audience: "ApplicateUsers",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

public record LoginRequest(string Username, string Password);