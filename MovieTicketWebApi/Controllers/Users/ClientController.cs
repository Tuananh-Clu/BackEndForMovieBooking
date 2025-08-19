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

        [Authorize]
        [HttpPost("Up")]
        public async Task<IActionResult> AddBooking(
            [FromBody] List<List<TicketInformation>> ticketInformation,
            [FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                {
                    return NotFound("User not found");
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
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetAll([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var data = await userCollection.Find(x => x.Id == userId).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(x => x.Id == userId).FirstOrDefaultAsync();

                if (data == null)
                    return NotFound("User not found");

                return Ok(new
                {
                    data.Name,
                    data.Email,
                    data.role,
                    data.tickets,
                    data.Point,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetUserData()
        {
            try
            {
                var data = await userCollection.Find(_ => true).ToListAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetQuantityTicket")]
        public async Task<IActionResult> GetQuantityTickets()
        {
            try
            {
                var userData = await userCollection.Find(_ => true).ToListAsync();
                var adminData = await adminCollection.Find(_ => true).ToListAsync();

                var userTicketCount = userData.SelectMany(user => user.tickets).SelectMany(ticket => ticket).Count();
                var adminTicketCount = adminData.SelectMany(admin => admin.tickets).SelectMany(ticket => ticket).Count();

                var totalTickets = userTicketCount + adminTicketCount;

                return Ok(totalTickets);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ API Error: " + ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetDoanhthuTicket")]
        public async Task<IActionResult> DoanhThu()
        {
            try
            {
                var userData = await userCollection.Find(_ => true).ToListAsync();
                var adminData = await adminCollection.Find(_ => true).ToListAsync();

                var userRevenue = userData.Sum(user => user.tickets.Sum(ticketGroup => ticketGroup.Sum(ticket => ticket.Price)));
                var adminRevenue = adminData.Sum(admin => admin.tickets.Sum(ticketGroup => ticketGroup.Sum(ticket => ticket.Price)));

                var totalRevenue = userRevenue + adminRevenue;

                return Ok(totalRevenue);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ API Error: " + ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("GetFavoriteMovies")]
        public async Task<IActionResult> GetFavoriteMovies(
            [FromBody] List<Movie> movieApiResponse,
            [FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                    return NotFound("User not found");

                var targetCollection = isAdmin ? adminCollection : userCollection;
                var update = Builders<Client>.Update.PushEach("YeuThich", movieApiResponse);

                var updateResult = await targetCollection.UpdateOneAsync(filter, update);

                return Ok(new { success = true, modifiedCount = updateResult.ModifiedCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ API Error: " + ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetFavouriteMovieByUser")]
        public async Task<IActionResult> GetFavouriteMoviesByUser([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound("User not found");

                var favoriteMovies = user.YeuThich?.Select(movie => new Movie
                {
                    id = movie.id,
                    title = movie.title,
                    poster = movie.poster,
                    duration = movie.duration
                }).Distinct().ToList() ?? new List<Movie>();

                return Ok(favoriteMovies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("DeleteUserFavorite")]
        public async Task<IActionResult> Delete(
            [FromHeader(Name = "Authorization")] string token,
            [FromQuery] string movieTitle)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                    return NotFound("User not found");

                var targetCollection = isAdmin ? adminCollection : userCollection;
                var updateFilter = Builders<Client>.Update.PullFilter(c => c.YeuThich, movie => movie.title == movieTitle);
                var result = await targetCollection.UpdateOneAsync(filter, updateFilter);

                return Ok(new { success = true, deletedCount = result.ModifiedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetQuantityTicketBuyByUserId")]
        public async Task<IActionResult> GetQuantity([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound("User not found");

                var quantity = user.tickets
                    .SelectMany(ticketGroup => ticketGroup)
                    .Where(ticket => ticket.Quantity > 0)
                    .Sum(ticket => ticket.Quantity); // Sum actual quantities instead of counting distinct tickets

                return Ok(quantity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetMovieByUserId")]
        public async Task<IActionResult> GetMovie([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync()
                        ?? await adminCollection.Find(filter).FirstOrDefaultAsync();

                if (user == null)
                    return NotFound("User not found");

                var uniqueMovieCount = user.tickets
                    .SelectMany(ticketGroup => ticketGroup)
                    .Select(ticket => ticket.MovieTitle)
                    .Where(title => !string.IsNullOrEmpty(title))
                    .Distinct()
                    .Count();

                return Ok(uniqueMovieCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("GetPointId")]
        public async Task<IActionResult> GetPointId([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var userId = ExtractUserIdFromToken(token);
                if (string.IsNullOrEmpty(userId))
                    return BadRequest("Unable to extract userId from token");

                var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
                var user = await userCollection.Find(filter).FirstOrDefaultAsync();
                var isAdmin = false;

                if (user == null)
                {
                    user = await adminCollection.Find(filter).FirstOrDefaultAsync();
                    isAdmin = true;
                }

                if (user == null)
                    return NotFound("User not found");

                const int POINT_PER_TICKET = 20;
                var totalTickets = user.tickets.Sum(ticketGroup => ticketGroup.Sum(ticket => ticket.Quantity));
                var userPoints = totalTickets * POINT_PER_TICKET;

                var targetCollection = isAdmin ? adminCollection : userCollection;
                var updateDefinition = Builders<Client>.Update.Set("Point", userPoints);
                await targetCollection.UpdateOneAsync(filter, updateDefinition);

                return Ok(userPoints);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // Helper method to extract user ID from JWT token
        private string ExtractUserIdFromToken(string token)
        {
            try
            {
                var jwt = token.Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(jwt);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}