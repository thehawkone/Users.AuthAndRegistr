using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Users.DataAccess;
using Users.DataAccess.Entites;

namespace Users.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly UsersDbContext _context;

    public AuthController(IConfiguration configuration, UsersDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (await _context.Users.AnyAsync(u => u.UserName == model.UserName)) {
            return BadRequest("Данный пользователь уже зарегистрирован");
        }
        
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

        var user = new UserEntity()
        {
            Id = Guid.NewGuid(),
            UserName = model.UserName,
            PasswordHash = passwordHash,
            Email = model.Email
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return Ok("Пользовать успешно зарегистрирован!");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName == model.UserName);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash)) {
            return Unauthorized("Пароль или логин неверный!");
        }

        var token = GenerateJwtToken(user.UserName);
        // return Ok(new { Token = token });
        return Ok("Авторизация выполнена успешно!");
    }
    
    private string GenerateJwtToken(string username)
    {
        var jwtSettings = _configuration.GetSection("Jwt");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public class RegisterModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
    }

    public class LoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
