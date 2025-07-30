using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Model.Ticket;
using MovieTicketWebApi.Model.User;
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
        public ClientController(IMongoClient client)
        {
            var database = client.GetDatabase("User");
            mongoCollection = database.GetCollection<Client>("Client");
        }
        [Authorize]
        [HttpPost("AddUser")]
        public async Task<IActionResult> CreateUser([FromBody] Client client)
        {
           var user= await mongoCollection.Find(i=>i.Id==client.Id).FirstOrDefaultAsync();
            if (user == null)
            {
                await mongoCollection.InsertOneAsync(client);
            }

            return Ok(new {sucess=true});
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


            foreach (var group in ticketInformation)
            {
                var update = Builders<Client>.Update.Push("tickets", group);
                var result = await mongoCollection.UpdateOneAsync(filter, update);
                Console.WriteLine($"Matched: {result.MatchedCount}, Modified: {result.ModifiedCount}");
            }

            return Ok(new { success = true });
        }
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetAll([FromHeader(Name ="Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(jwt);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var data = await mongoCollection.Find(x=>x.Id==userId).FirstOrDefaultAsync();

            return Ok(new
                {
                data.Name,
                data.Email,
                data.tickets
            });
        }
    }

}
