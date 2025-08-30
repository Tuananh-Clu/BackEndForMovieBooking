using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Dto;
using MovieTicketWebApi.Model.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IMongoCollection<Client> mongoCollection;
    private readonly IConfiguration configuration;

    public AuthController(MongoDbContext db, IConfiguration configurations)
    {
        mongoCollection = db.User;
        configuration = configurations;
    }

    private string GenerateToken(Client user)
    {
        var jwtKey = configuration["Jwt:Key"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim(ClaimTypes.Role, user.role),
            new Claim("avatar", user.Avatar ?? "")
        };

        var token = new JwtSecurityToken(
            issuer: "MovieApi",
            audience: "MovieApiClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var filter = Builders<Client>.Filter.And(
            Builders<Client>.Filter.Eq(a => a.Email, loginDto.Email),
            Builders<Client>.Filter.Eq(a => a.Name, loginDto.UserName));

        var data = await mongoCollection.Find(filter).FirstOrDefaultAsync();
        if (data == null) return NotFound("Không tìm thấy user");

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, data.PassWord))
            return BadRequest("Sai mật khẩu");

        var token = GenerateToken(data);

        return Ok(new
        {
            Token = token,
            User = new { data.Id, data.Name, data.Email, data.role }
        });
    }

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUp signUp)
    {
        var exsist = await mongoCollection
            .Find(Builders<Client>.Filter.Eq(a => a.Email, signUp.Email))
            .FirstOrDefaultAsync();

        if (exsist != null)
            return BadRequest("Email đã tồn tại");

        var newUser = new Client
        {
            Name = signUp.Username,
            Email = signUp.Email,
            PassWord = BCrypt.Net.BCrypt.HashPassword(signUp.Password),
            Avatar = signUp.AvatarUrl,
            role = "User"
        };

        await mongoCollection.InsertOneAsync(newUser);

        var token = GenerateToken(newUser);

        return Ok(new
        {
            Token = token,
            User = new { newUser.Id, newUser.Name, newUser.Email, newUser.role }
        });
    }
}
