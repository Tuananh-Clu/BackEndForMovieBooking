using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTicketWebApi.Service;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieNowPlayingController : ControllerBase
    {
        public readonly MoviePlayingTmdbApi moviePlayingTmdbApi;

        public MovieNowPlayingController(MoviePlayingTmdbApi playingTmdbApi)
        {
            moviePlayingTmdbApi = playingTmdbApi;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var movie = await moviePlayingTmdbApi.getAll();
            return Ok(movie);
        }

        [HttpGet("SaveMoviePlaying")]
        public async Task<IActionResult> Save()
        {
            await moviePlayingTmdbApi.SaveAllToMongoDb();
            return Ok("Đã lưu danh sách phim đang chiếu vào MongoDB.");
        }
        [HttpGet("Show")]
        public async Task<IActionResult> Show()
        {
            var data = await moviePlayingTmdbApi.Show();
            return Ok(data);
        }
    }
}
