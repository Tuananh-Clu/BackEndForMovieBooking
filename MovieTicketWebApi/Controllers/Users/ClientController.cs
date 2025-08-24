using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model;
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
        public readonly IMongoCollection<VoucherDb> mongo;

        public ClientController(MongoDbContext dbContext)
        {

            mongoCollection = dbContext.User;
            collection = dbContext.Admin;
            mongo = dbContext.Voucher;



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

            });
        }
        [Authorize]
        [HttpPost("GetFavoriteMovies")]
        public async Task<IActionResult> GetFavoriteMovies([FromHeader(Name ="Authorization")] string token, [FromBody] List<Movie> movieApiResponse)
        {
            if (movieApiResponse == null || movieApiResponse.Count == 0)
                return BadRequest("Body cannot be empty");

            try
            {
                var dat = token.Replace("Bearer ", "");
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dat);
                var userid = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                if (string.IsNullOrEmpty(userid))
                    return Unauthorized("Invalid token");

                var user = await mongoCollection.Find(x => x.Id == userid).FirstOrDefaultAsync();
                if (user == null) return NotFound("Không tìm thấy người dùng");

                var update = Builders<Client>.Update.PushEach("YeuThich", movieApiResponse);

                await mongoCollection.UpdateOneAsync(x => x.Id == userid, update);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Swagger API Error: " + ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }
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
                var userLength = data.SelectMany(user => user.tickets).SelectMany(ticket => ticket).Select(a=>a.Quantity>0).Count();
                return Ok(userLength);
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


        [Authorize]
        [HttpGet("GetRapPhimYeuThichNhat")]
        public async Task<IActionResult> GetRapYeuThich([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var data = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            if (data == null) return NotFound("Không tìm thấy người dùng");
            var rapYeuThichNhat = data.tickets.SelectMany(h => h).GroupBy(ticket => ticket.City)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
            return Ok(rapYeuThichNhat);
        }
        [Authorize]
        [HttpGet("GetTicketsDaXem")]
        public async Task<IActionResult> GetTicketsDaXem([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var user = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            if (user == null) return NotFound("Không tìm thấy người dùng");
            var data = user.tickets.SelectMany(h => h).Where(user=>DateTime.Parse(user.Date) < DateTime.Now)
                .Select(ticket => new Movie
                {
                    id =ticket.Id,
                    title = ticket.MovieTitle,
                    poster = ticket.Image,
                    duration = 120
                }).GroupBy(t => t.title).Select(r => r.First()).Distinct().ToList();
        
            return Ok(data);
        }
        [Authorize]
        [HttpGet("GetTicketsSapChieu")]
        public async Task<IActionResult> GetTicketsSapChieu([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(c => c.Type == "sub")?.Value;
            var filter = Builders<Client>.Filter.Eq(c => c.Id, userid);
            var user = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            if (user == null) return NotFound("Không tìm thấy người dùng");
            var data = user.tickets.SelectMany(h => h).Where(user => DateTime.Parse(user.Date) >= DateTime.Now)
                .Select(ticket => new Movie
                {
                    id = ticket.Id,
                    title = ticket.MovieTitle,
                    poster = ticket.Image,
                    duration = 120
                }).GroupBy(t=>t.title).Select(r=>r.First()).Distinct().ToList();

            return Ok(data);
        }
        [Authorize]
        [HttpPost("AddVoucher")]
        public async Task<IActionResult> AddVoucher([FromHeader(Name ="Authorization")] string token, [FromBody]List<VoucherForUser> voucherForUsers)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(n => n.Type == "sub")
                ?.Value;
            var filter = Builders<Client>.Filter.Eq(a => a.Id, userid);
            var data = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            var code = voucherForUsers.Select(a => a.Code).FirstOrDefault();
            var match = data.VoucherCuaBan.Select(a => a.Code == code);
            string notice;
            if (match != null) {
                notice = "Không Thể Thêm Vé Bị Trùng";
            }
            else
            {
                var update = Builders<Client>.Update.PushEach("VoucherCuaBan", voucherForUsers);
                await mongoCollection.UpdateOneAsync(filter, update);
                notice = "Thêm Vé Thành Công";
            }
            return Ok(notice);
            
            
        }
        [Authorize]
        [HttpGet("GetVoucher")]
        public async Task<IActionResult> GetVoucher([FromHeader(Name = "Authorization")] string token)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(n => n.Type == "sub")
                ?.Value;
            var filter = Builders<Client>.Filter.Eq(a => a.Id, userid);
            var user = await mongoCollection.Find(filter).ToListAsync();
            var data = user.SelectMany(a => a.VoucherCuaBan).Select(s=>s).Where(s=>s.used=="DangGiu").GroupBy(a=>a.Code).Select(g=>g.First()).Distinct().ToList();
            return Ok(data);
        }
        [Authorize]
        [HttpPost("Used")]
        public async Task<IActionResult> DaSuDung([FromHeader(Name = "Authorization")] string token, [FromQuery]string code)
        {
            var jwt = token.Replace("Bearer ", "");
            var userid = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(n => n.Type == "sub")
                ?.Value;
            var filter = Builders<Client>.Filter.And(
                Builders<Client>.Filter.Eq(a => a.Id, userid),
                Builders<Client>.Filter.ElemMatch(a => a.VoucherCuaBan,s=>s.Code==code));
            var match = Builders<VoucherDb>.Filter.Eq(a => a.Code, code);
            var up = Builders<VoucherDb>.Update.Inc(a => a.UsageCount, -1);
            var data = await mongoCollection.Find(filter).FirstOrDefaultAsync();
            var fetchs = data.VoucherCuaBan.Any(a => a.SoLuotUserDuocDung == "1 lần");
            if (fetchs)
            {
                var update = Builders<Client>.Update.Set("VoucherCuaBan.$.used", "DaSuDung");
                await mongoCollection.UpdateOneAsync(filter, update);
            }
            else {
                var update = Builders<Client>.Update.Set("VoucherCuaBan.$.used", "DangGiu");
                await mongoCollection.UpdateOneAsync(filter, update);
            }

            await mongo.UpdateOneAsync(match, up);
            return Ok("Success");


        }
        [Authorize]
        [HttpGet("GetVoucherByCode")]
        public async Task<IActionResult> GetVoucherByCode([FromHeader(Name = "Authorization")] string token, [FromQuery] string? code)
        {
            var jwt = token.Replace("Bearer ", "");
            var userId = new JwtSecurityTokenHandler()
                .ReadJwtToken(jwt)
                .Claims
                .FirstOrDefault(a => a.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Invalid token");

            var filter = Builders<Client>.Filter.Eq(a => a.Id, userId);
            var users = await mongoCollection.Find(filter).ToListAsync();

            if (users == null || users.Count == 0)
                return NotFound("User not found");

            var vouchers = users
                .Where(c => c.VoucherCuaBan != null)
                .SelectMany(c => c.VoucherCuaBan);

            if (string.IsNullOrEmpty(code))
            {
                var allVouchers = vouchers
                    .GroupBy(s => s.Code)
                    .Select(g => g.First())
                    .ToList();
                return Ok(allVouchers);
            }

            var filteredVouchers = vouchers
                .Where(a => !string.IsNullOrEmpty(a.Code) && a.Code.Contains(code))
                .GroupBy(s => s.Code)
                .Select(g => g.First())
                .ToList();

            return Ok(filteredVouchers);
        }
    }

}