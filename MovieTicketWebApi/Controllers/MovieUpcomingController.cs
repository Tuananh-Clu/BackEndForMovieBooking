using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Service;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieUpcomingController : ControllerBase
    {
        public readonly MovieUpcomingTmdbApi service;
        public MovieUpcomingController(MovieUpcomingTmdbApi upcomingTmdbApi)
        {

            service = upcomingTmdbApi;
        }

        [HttpGet("GetMovie")]
        public async Task<IActionResult> GetData()
        {

            var result = await service.GetFromTmdb();
            return Ok(result);
        }
        [HttpGet("Show")]
        public async Task<IActionResult> Show()
        {
            return Ok(await service.ShowAll());
        }
        [HttpGet("Save")]
        public async Task<IActionResult> save()
        {
            await service.saveMongodb();
            return Ok();
        }
    }
}
