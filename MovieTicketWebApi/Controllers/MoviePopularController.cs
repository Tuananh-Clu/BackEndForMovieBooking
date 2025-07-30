using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTicketWebApi.Service;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviePopularController : ControllerBase
    {
        public readonly MoviePopularTmdbApi_cs moviePopularTmdbApi_Cs;
        public MoviePopularController(MoviePopularTmdbApi_cs tmdbApi_Cs)
        {
            moviePopularTmdbApi_Cs = tmdbApi_Cs;
        }
        [HttpGet("Get-PopularMovie")]
        public async Task<IActionResult> GetMovie()
        {
            var movie =await moviePopularTmdbApi_Cs.GetMovie();
            return Ok(movie);
        }
        [HttpGet("Save-PopularMovie")]
        public async Task<IActionResult> SaveAll()
        {
             await moviePopularTmdbApi_Cs.SaveAllMoviePopular();
            return Ok();
        }
        [HttpGet("Show")]
        public async Task<IActionResult> Show()
        {
            var movie = await moviePopularTmdbApi_Cs.ShowAll();
            return Ok(movie);
        }
    }
}
