using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
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
        public readonly IMongoCollection<Client> collection;
        public ClientController(MongoDbContext dbContext)
        {

            mongoCollection = dbContext.User;
            collection = dbContext.Admin;


        }
        [Authorize]
        [HttpPost("AddUser")]
        public async Task<IActionResult> CreateUser([FromBody] Client client)
        {
            var user = await mongoCollection.Find(i => i.Id == client.Id).FirstOrDefaultAsync();
            if (user == null&&client.role=="User")
            {
                await mongoCollection.InsertOneAsync(client);
            }
            else if(user==null&&client.role=="Admin") {
                await collection.InsertOneAsync(client);
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
               
            });
        }
        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetUserData()
        {
            var data=await mongoCollection.Find(_=>true).ToListAsync();
            return Ok(data);
        }

    }

}
