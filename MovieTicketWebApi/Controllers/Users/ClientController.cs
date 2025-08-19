using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Model.Ticket;
using MovieTicketWebApi.Model.User;
using MovieTicketWebApi.Service;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Security.Claims;

namespace MovieTicketWebApi.Controllers.User
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IMongoCollection<Client> userCollection;
        private readonly IMongoCollection<Client> adminCollection;

        public ClientController(MongoDbContext dbContext)
        {
            userCollection = dbContext.User;
            adminCollection = dbContext.Admin;
        }

        [HttpPost("AddUser")]
        public async Task<IActionResult> CreateUser([FromBody] Client client)
        {
            try
            {
                // Check if user already exists in both collections
                var existingUser = await userCollection.Find(i => i.Id == client.Id).FirstOrDefaultAsync();
                var existingAdmin = await adminCollection.Find(i => i.Id == client.Id).FirstOrDefaultAsync();

                if (existingUser != null || existingAdmin != null)
                {
                    return BadRequest(new { success = false, message = "User already exists" });
                }

                if (client.role == "User")
                {
                    await userCollection.InsertOneAsync(client);
                }
                else if (client.role == "Admin")
                {
                    await adminCollection.InsertOneAsync(client);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid role" });
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // DEBUG: Test token validation without authorize
        [HttpGet("TestToken")]
        public async Task<IActionResult> TestToken([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                Console.WriteLine($"📝 Received token: {token}");

                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { error = "No token provided" });
                }

                if (!token.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Token must start with 'Bearer '" });
                }

                var jwt = token.Replace("Bearer ", "");
                Console.WriteLine($"📝 JWT after Bearer removal: {jwt}");

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(jwt);

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                var exp = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                var iss = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;

                Console.WriteLine($"📝 UserId from token: {userId}");
                Console.WriteLine($"📝 Token expiry: {exp}");
                Console.WriteLine($"📝 Token issuer: {iss}");

                // Check if token is expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    return BadRequest(new { error = "Token has expired", expiry = jwtToken.ValidTo });
                }

                return Ok(new
                {
                    success = true,
                    userId = userId,
                    expiry = jwtToken.ValidTo,
                    issuer = iss,
                    allClaims = jwtToken.Claims.Select(c => new { c.Type, c.Value })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Token validation error: {ex.Message}");
                return BadRequest(new { error = "Invalid token", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("Up")]
        public async Task<IActionResult> AddBooking(
            [FromBody] List<List<TicketInformation>> ticketInformation,
            [FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                // Alternative: Get userId from HttpContext instead of manually parsing token
                var userIdFromContext = HttpContext.User.FindFirst("sub")?.Value
                                     ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                Console.WriteLine($"📝 UserId from HttpContext: {userIdFromContext}");

                var userId = ExtractUserIdFromToken(token);
                Console.WriteLine($"📝 UserId from token parsing: {userId}");

                if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userIdFromContext))
                {
                    return BadRequest(new { success = false, message = "Unable to extract userId" });
                }

                // Use HttpContext userId if available, fallback to token parsing
                var finalUserId = userIdFromContext ?? userId;

                var filter = Builders<Client>.Filter.Eq(c => c.Id, finalUserId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                var targetCollection = isAdmin ? adminCollection : userCollection;

                foreach (var group in ticketInformation)
                {
                    var update = Builders<Client>.Update.Push("tickets", group);
                    await targetCollection.UpdateOneAsync(filter, update);
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AddBooking error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetAll([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                // Try multiple ways to get userId
                var userIdFromContext = HttpContext.User.FindFirst("sub")?.Value
                                     ?? HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                     ?? HttpContext.User.FindFirst("userId")?.Value;

                var userIdFromToken = ExtractUserIdFromToken(token);

                var userId = userIdFromContext ?? userIdFromToken;

                Console.WriteLine($"📝 Context UserId: {userIdFromContext}");
                Console.WriteLine($"📝 Token UserId: {userIdFromToken}");
                Console.WriteLine($"📝 Final UserId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { success = false, message = "Unable to extract userId" });
                }

                var data = await userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

                if (data == null)
                    return NotFound(new { success = false, message = "User not found" });

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        data.Name,
                        data.Email,
                        data.role,
                        data.tickets,
                        data.Point,
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetUser error: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetUserData()
        {
            try
            {
                var data = await userCollection.Find(_ => true).ToListAsync();
                return Ok(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("GetQuantityTicket")]
        public async Task<IActionResult> GetQuantityTickets()
        {
            try
            {
                var userData = await userCollection.Find(_ => true).ToListAsync();
                var adminData = await adminCollection.Find(_ => true).ToListAsync();

                var userTicketCount = userData.SelectMany(user => user.tickets ?? new List<List<TicketInformation>>())
                                             .SelectMany(ticket => ticket ?? new List<TicketInformation>())
                                             .Count();

                var adminTicketCount = adminData.SelectMany(admin => admin.tickets ?? new List<List<TicketInformation>>())
                                                .SelectMany(ticket => ticket ?? new List<TicketInformation>())
                                                .Count();

                var totalTickets = userTicketCount + adminTicketCount;

                return Ok(new { success = true, totalTickets = totalTickets });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ API Error: " + ex.Message);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("GetDoanhthuTicket")]
        public async Task<IActionResult> DoanhThu()
        {
            try
            {
                var userData = await userCollection.Find(_ => true).ToListAsync();
                var adminData = await adminCollection.Find(_ => true).ToListAsync();

                var userRevenue = userData.Sum(user =>
                    (user.tickets ?? new List<List<TicketInformation>>())
                    .Sum(ticketGroup =>
                        (ticketGroup ?? new List<TicketInformation>())
                        .Sum(ticket => ticket.Price)));

                var adminRevenue = adminData.Sum(admin =>
                    (admin.tickets ?? new List<List<TicketInformation>>())
                    .Sum(ticketGroup =>
                        (ticketGroup ?? new List<TicketInformation>())
                        .Sum(ticket => ticket.Price)));

                var totalRevenue = userRevenue + adminRevenue;

                return Ok(new { success = true, totalRevenue = totalRevenue });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ API Error: " + ex.Message);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // Temporarily remove [Authorize] for testing
        [HttpPost("GetFavoriteMovies")]
        public async Task<IActionResult> GetFavoriteMovies(
            [FromBody] List<Movie> movieApiResponse,
            [FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { success = false, message = "No authorization token provided" });
                }

                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Unable to extract userId from token" });

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var targetCollection = isAdmin ? adminCollection : userCollection;
                var update = Builders<Client>.Update.PushEach("YeuThich", movieApiResponse);

                var updateResult = await targetCollection.UpdateOneAsync(filter, update);

                return Ok(new { success = true, modifiedCount = updateResult.ModifiedCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ API Error: " + ex.Message);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetFavouriteMovieByUser")]
        public async Task<IActionResult> GetFavouriteMoviesByUser([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token) ?? HttpContext.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Unable to extract userId" });

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var favoriteMovies = user.YeuThich?.Select(movie => new Movie
                {
                    id = movie.id,
                    title = movie.title,
                    poster = movie.poster,
                    duration = movie.duration
                }).Distinct().ToList() ?? new List<Movie>();

                return Ok(new { success = true, movies = favoriteMovies });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // Helper method to extract user ID from JWT token with better error handling
        private string ExtractUserIdFromToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("❌ Token is null or empty");
                    return null;
                }

                if (!token.StartsWith("Bearer "))
                {
                    Console.WriteLine("❌ Token doesn't start with 'Bearer '");
                    return null;
                }

                var jwt = token.Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(jwt))
                {
                    Console.WriteLine("❌ Cannot read JWT token");
                    return null;
                }

                var jwtToken = handler.ReadJwtToken(jwt);

                // Check if token is expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    Console.WriteLine($"❌ Token expired at {jwtToken.ValidTo}, current time: {DateTime.UtcNow}");
                    return null;
                }

                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                Console.WriteLine($"📝 Extracted userId: {userId}");

                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error extracting userId from token: {ex.Message}");
                return null;
            }
        }

        // Continue with other methods...
        [Authorize]
        [HttpDelete("DeleteUserFavorite")]
        public async Task<IActionResult> Delete(
            [FromHeader(Name = "Authorization")] string token,
            [FromQuery] string movieTitle)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token) ?? HttpContext.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Unable to extract userId" });

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var targetCollection = isAdmin ? adminCollection : userCollection;
                var updateFilter = Builders<Client>.Update.PullFilter(c => c.YeuThich, movie => movie.title == movieTitle);
                var result = await targetCollection.UpdateOneAsync(filter, updateFilter);

                return Ok(new { success = true, deletedCount = result.ModifiedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetQuantityTicketBuyByUserId")]
        public async Task<IActionResult> GetQuantity([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token) ?? HttpContext.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Unable to extract userId" });

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var quantity = (user.tickets ?? new List<List<TicketInformation>>())
                    .SelectMany(ticketGroup => ticketGroup ?? new List<TicketInformation>())
                    .Where(ticket => ticket.Quantity > 0)
                    .Sum(ticket => ticket.Quantity);

                return Ok(new { success = true, quantity = quantity });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetMovieByUserId")]
        public async Task<IActionResult> GetMovie([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token) ?? HttpContext.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Unable to extract userId" });

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var uniqueMovieCount = (user.tickets ?? new List<List<TicketInformation>>())
                    .SelectMany(ticketGroup => ticketGroup ?? new List<TicketInformation>())
                    .Select(ticket => ticket.MovieTitle)
                    .Where(title => !string.IsNullOrEmpty(title))
                    .Distinct()
                    .Count();

                return Ok(new { success = true, movieCount = uniqueMovieCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetPointId")]
        public async Task<IActionResult> GetPointId([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token) ?? HttpContext.User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { success = false, message = "Unable to extract userId" });

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                const int POINT_PER_TICKET = 20;
                var totalTickets = (user.tickets ?? new List<List<TicketInformation>>())
                    .Sum(ticketGroup =>
                        (ticketGroup ?? new List<TicketInformation>())
                        .Sum(ticket => ticket.Quantity));

                var userPoints = totalTickets * POINT_PER_TICKET;

                var targetCollection = isAdmin ? adminCollection : userCollection;
                var updateDefinition = Builders<Client>.Update.Set("Point", userPoints);
                await targetCollection.UpdateOneAsync(filter, updateDefinition);

                return Ok(new { success = true, points = userPoints });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}