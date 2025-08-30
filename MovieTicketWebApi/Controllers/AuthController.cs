using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MovieTicketWebApi.Model.Ticket;
using MovieTicketWebApi.Model.Cinema;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<Client> _userCollection;
        private readonly IMongoCollection<Client> _adminCollection;

        public AuthController(MongoDbContext dbContext)
        {
            _userCollection = dbContext.User;
            _adminCollection = dbContext.Admin;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {

                if (request == null)
                    return BadRequest(new { success = false, message = "Dữ liệu đăng ký không được để trống" });



                var existingUser = await _userCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (existingUser != null)
                    return BadRequest(new { success = false, message = "Email đã tồn tại trong hệ thống" });

                var existingAdmin = await _adminCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (existingAdmin != null)
                    return BadRequest(new { success = false, message = "Email đã tồn tại trong hệ thống" });


                var newUser = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = request.Email,
                    PassWord = request.Password,
                    Name = request.Name,
                    role = "User",
                    Point = 0,
                    tickets = new List<List<TicketInformation>>(),
                    YeuThich = new List<Movie>(),
                    VoucherCuaBan = new List<VoucherForUser>(),
                    Tier = "Bronze"
                };

                await _userCollection.InsertOneAsync(newUser);

                return Ok(new { 
                    success = true, 
                    message = "Đăng ký thành công", 
                    userId = newUser.Id,
                    user = new
                    {
                        id = newUser.Id,
                        email = newUser.Email,
                        name = newUser.Name,
                        role = newUser.role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi đăng ký: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                    return BadRequest(new { success = false, message = "Email và mật khẩu không được để trống" });

                var user = await _userCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (user != null && user.PassWord == request.Password)
                {
                    var token = GenerateJwtToken(user);
                    return Ok(new { 
                        success = true, 
                        message = "Đăng nhập thành công", 
                        token = token,
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            name = user.Name,
                            role = user.role,
                            point = user.Point,
                            tier = user.Tier
                        }
                    });
                }
                var admin = await _adminCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (admin != null && admin.PassWord == request.Password)
                {
                    var token = GenerateJwtToken(admin);
                    return Ok(new { 
                        success = true, 
                        message = "Đăng nhập admin thành công", 
                        token = token,
                        user = new
                        {
                            id = admin.Id,
                            email = admin.Email,
                            name = admin.Name,
                            role = admin.role,
                            point = admin.Point,
                            tier = admin.Tier
                        }
                    });
                }

                return Unauthorized(new { success = false, message = "Email hoặc mật khẩu không đúng" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi đăng nhập: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        [HttpPost("AdminRegister")]
        public async Task<IActionResult> AdminRegister([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate input
                if (request == null)
                    return BadRequest(new { success = false, message = "Dữ liệu đăng ký không được để trống" });


                // Check if admin already exists by email
                var existingUser = await _userCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (existingUser != null)
                    return BadRequest(new { success = false, message = "Email đã tồn tại trong hệ thống" });

                var existingAdmin = await _adminCollection.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (existingAdmin != null)
                    return BadRequest(new { success = false, message = "Email đã tồn tại trong hệ thống" });

                // Create new admin
                var newAdmin = new Client
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = request.Email,
                    PassWord = request.Password, 
                    Name = request.Name,
                    role = "Admin",
                    Point = 0,
                    tickets = new List<List<TicketInformation>>(),
                    YeuThich = new List<Movie>(),
                    VoucherCuaBan = new List<VoucherForUser>(),
                    Tier = "Admin"
                };

                await _adminCollection.InsertOneAsync(newAdmin);

                return Ok(new { 
                    success = true, 
                    message = "Đăng ký admin thành công", 
                    userId = newAdmin.Id,
                    user = new
                    {
                        id = newAdmin.Id,
                        email = newAdmin.Email,
                        name = newAdmin.Name,
                        role = newAdmin.role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi đăng ký admin: " + ex.Message);
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        private string GenerateJwtToken(Client user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("kmwxPCZP4RyF+TW6BQmpsmqtkZSESJRDIAMxPHrO56g="); 
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.role),
                    new Claim(ClaimTypes.Name, user.Name ?? "")
                }),
                Expires = DateTime.UtcNow.AddDays(7), 
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
