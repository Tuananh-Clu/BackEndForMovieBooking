using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Service;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageMovieController : ControllerBase
    {
        public readonly StorageMovieTmdb service;
        public readonly IMongoCollection<MoviesInfomation> mongo;
        public StorageMovieController(StorageMovieTmdb storageMovieTmdb)
        {
            service = storageMovieTmdb;

        }
        [HttpGet("get_Page")]
        public async Task<IActionResult> GetAll()
        {
            var movie = await service.Get();
            return Ok(movie);   
        }
        [HttpGet("Save")]
        public async Task<IActionResult> SaveMongo()
        {
             await service.SaveToMongoDb();
            return Ok();
        }
        [HttpGet("ShowAll")]
        public async Task<IActionResult> ShowAll()
        {
            var data = await service.Show();
            return Ok(data);
        }

    }
}
