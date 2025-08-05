using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Model.Ticket;
using MovieTicketWebApi.Service;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;


namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CinemaController : ControllerBase
    {
        public readonly CinemaService cinemaService;

        public CinemaController(CinemaService service)
        {
            cinemaService = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
          
                try
                {
                    var result = await cinemaService.GetCinemasAsync();
                    return Ok(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Swagger API Error: " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
                }
            

        }

        [HttpPost("Read_Json")]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> readFile(IFormFile file, [FromQuery]bool isChecked)
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var json = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<List<Cinema>>(json);
            await cinemaService.InsertAsync(result,isChecked);
            return Ok(result);
        }
        [HttpPost("Filter_movie")]
        public async Task<IActionResult> Filter([FromQuery]string movie,[FromBody]List<Cinema> cinemas)
        {
            var data = cinemas.Where(x => x.city.Contains(movie)).ToList();
            return Ok(data);
        }
        [HttpPost("Update")]
        public async Task<IActionResult> Updates(List<TicketInformation> ticketInformation)
        {
            await cinemaService.Update(ticketInformation);
            return Ok(ticketInformation);
        }
        [HttpGet("MovieBooking")]
        public async Task<IActionResult> getMovieBooking()
        {
            var data =await cinemaService.GetMovieBooking();
            return Ok(data.ToList());
        }
    }
}
