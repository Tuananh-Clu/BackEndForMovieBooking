using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Dto;
using MovieTicketWebApi.Model.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Auth : ControllerBase
    {
        public readonly IMongoCollection<Client> mongoCollection;
        public readonly IConfiguration configuration;
        public Auth(MongoDbContext db,IConfiguration configurations)
        {
            mongoCollection = db.User;
            configuration = configurations;
        }
        public string GenerateToken(Client user) {
            var jwtKey = configuration["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds=new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name",user.Name),
                new Claim(ClaimTypes.Role,user.role),
                new Claim("avatar",user.Avatar)
            };
            var token = new JwtSecurityToken(
                issuer: "MovieApi",
                audience: "MovieApiClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds);
                ;
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var filter = Builders<Client>.Filter.And(
                Builders<Client>.Filter.Eq(a => a.Email, loginDto.Email),
                Builders<Client>.Filter.Eq(a => a.Name, loginDto.UserName)) ;
            if (filter == null) {
                return NotFound();
            }
            var data=await mongoCollection.Find(filter).FirstOrDefaultAsync();
            if(!BCrypt.Net.BCrypt.Verify(data.PassWord,loginDto.Password))
            {
                return NotFound("Sai Mat Khau");
            }
            var token=GenerateToken(data);
            return Ok(new
            {
                Token = token,
                User = new { data.Id, data.Name, data.Email, data.role }
            });

        }
        [HttpPost]
        public async Task<IActionResult> SignUp([FromBody] SignUp signUp)
        {
            var exsist = Builders<Client>.Filter.Eq(a => a.Email, signUp.Email);
            if (exsist == null)
            {
                return NotFound();
            }
            var newUser = new Client
            {
                Name = signUp.Username,
                Email = signUp.Email,
                PassWord = BCrypt.Net.BCrypt.HashPassword(signUp.Password),
                Avatar = signUp.AvatarUrl,
                role = "User"
            };
            await mongoCollection.InsertOneAsync(newUser);
            var data = await mongoCollection.Find(exsist).FirstOrDefaultAsync();
            var token = GenerateToken(newUser);
            return Ok(new
            {
                Token = token,
                user = new Client
                {
                    Name = signUp.Username,
                    Email = signUp.Email,
                    Avatar = signUp.AvatarUrl,
                }
            });
        }

           
    }
}
