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
        public readonly IMongoCollection<Client> mongoCollection;
        public readonly IMongoCollection<Client> collection;

        public ClientController(MongoDbContext dbContext)
        {

            mongoCollection = dbContext.User;
            collection = dbContext.Admin;



        }
        [HttpPost("AddUser")]
        public async Task<IActionResult> CreateUser([FromBody] Client client)
        {
            var user = await mongoCollection.Find(i => i.Id == client.Id).FirstOrDefaultAsync();
            if (user == null && client.role == "User")
            {
                await mongoCollection.InsertOneAsync(client);
            }
            else if (user == null && client.role == "Admin")
            {
                await collection.InsertOneAsync(client);
            }

            return Ok(new { sucess = true });
        }

        [Authorize]
        [HttpPost("Up")]
        public async Task<IActionResult> AddBooking(
     [FromBody] List<List<TicketInformation>> ticketInformation,
     [FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(jwt);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Không tìm thấy userId trong token");

            var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);

            var user = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            if (user == null)
            {
                user = await collection.Find(filter).FirstOrDefaultAsync();
                foreach (var group in ticketInformation)
                {
                    var update = Builders<Client>.Update.Push("tickets", group);
                    var result = await collection.UpdateOneAsync(filter, update);
                }

            }
            else
            {
                foreach (var group in ticketInformation)
                {
                    var update = Builders<Client>.Update.Push("tickets", group);
                    var result = await mongoCollection.UpdateOneAsync(filter, update);
                }
            }



            return Ok(new { success = true });
        }
        [Authorize]
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetAll([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userId = new JwtSecurityTokenHandler()
                            .ReadJwtToken(jwt)
                            .Claims
                            .FirstOrDefault(c => c.Type == "sub")?.Value;

            var data = await mongoCollection.Find(x => x.Id == userId).FirstOrDefaultAsync()
                    ?? await collection.Find(x => x.Id == userId).FirstOrDefaultAsync();

            if (data == null) return NotFound("Không tìm thấy người dùng");

            return Ok(new
            {
                data.Name,
                data.Email,
                data.role,
                data.tickets,
                data.Point,

            });
        }


        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetUserData()
        {
            var data = await mongoCollection.Find(_ => true).ToListAsync();
            return Ok(data);
        }

        [HttpGet("GetQuantityTicket")]
        public async Task<IActionResult> GetQuantityTickets()
        {
            try
            {
                var data = await mongoCollection.Find(_ => true).ToListAsync();
                var userLength = data.SelectMany(user => user.tickets).SelectMany(ticket => ticket).Count();
                return Ok(userLength - 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Swagger API Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpGet("GetDoanhthuTicket")]
        public async Task<IActionResult> DoanhThu()
        {
            try
            {
                var data = await mongoCollection.Find(_ => true).ToListAsync();
                var datauser = data.Sum(user => user.tickets.Sum(h => h.Sum(ticket => ticket.Price)));
                return Ok(datauser);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Swagger API Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });

            }

        }
        [Authorize]
        [HttpPost("GetFavoriteMovies")]
        public async Task<IActionResult> GetFavoriteMovies(List<Movie> movieApiResponse, [FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                var jwt = token.Replace("Bearer ", "");
                var userid = new JwtSecurityTokenHandler()
                    .ReadJwtToken(jwt)
                    .Claims
                    .FirstOrDefault(c => c.Type == "sub")?.Value;
                var data = await mongoCollection.Find(x => x.Id == userid).FirstOrDefaultAsync();
                var admin = data == null ? await collection.Find(x => x.Id == userid).FirstOrDefaultAsync() : null;
                var result = Builders<Client>.Update.PushEach("YeuThich", movieApiResponse);

                var updateResult = data != null ? await mongoCollection.UpdateOneAsync(
                    x => x.Id == userid,
                    result
                ) : await collection.UpdateOneAsync(
                    x => x.Id == userid,
                    result
                );
                if (data == null) return NotFound("Không tìm thấy người dùng");
                return Ok(updateResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Swagger API Error: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }

        }
        [Authorize]
        [HttpGet("GetFavouriteMovieByUser")]
        public async Task<IActionResult> GetFavouriteMoviesByUser([FromHeader(Name = "Authorization")] string token)
        {
            var jwtToken = token.Replace("Bearer ", "");
            var userId = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwtToken)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;

            var filter = Builders<Client>.Filter.Eq(c => c.Id, userId);
            var user = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            var favoriteMovies = user.YeuThich?.Select(h => new Movie
            {
                id = h.id,
                title = h.title,
                poster = h.poster,
                duration = h.duration
            }).Distinct().ToList();

            return Ok(favoriteMovies);
        }
        [Authorize]
        [HttpDelete("DeleteUserFavorite")]
        public async Task Delete([FromHeader(Name = "Authorization")] string token, [FromQuery] string movieTitle)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler().ReadJwtToken(jwt).Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var user = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            var updateFilter = Builders<Client>.Update.PullFilter(c => c.YeuThich, h => h.title == movieTitle);
            var update = await mongoCollection.UpdateOneAsync(filter, updateFilter);

        }
        [Authorize]
        [HttpGet("GetQuantityTIcketBuyByUserId")]
        public async Task<IActionResult> getQuantity([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var user = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            var quantity = user.tickets.Select(h => h).SelectMany(ticket => ticket).Where(a => a.Quantity > 0).Distinct().Count();
            return Ok(quantity);
        }
        [Authorize]
        [HttpGet("GetMovieByUserId")]
        public async Task<IActionResult> GetMovie([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var data = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            var movie = data.tickets.SelectMany(h => h).Select(data => data.MovieTitle).Distinct().Count();
            return Ok(movie);
        }
        [Authorize]
        [HttpGet("GetPointId")]
        public async Task<IActionResult> GetPointId([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var data = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            const int POINT_PER_TICKET = 20;
            var userponit = data.tickets.Sum(h => h.Sum(ticket => ticket.Quantity) * POINT_PER_TICKET);
            var lol = Builders<Client>.Update.Set("Point", userponit);
            var update = await mongoCollection.UpdateOneAsync(filter, lol);
            return Ok(userponit);
        }

    }

}