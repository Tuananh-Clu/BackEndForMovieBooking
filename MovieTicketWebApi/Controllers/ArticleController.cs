using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Model.Article;
using MovieTicketWebApi.Service.Article;

namespace MovieTicketWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly ArticleService ArticleService;
        private readonly IMongoCollection<MainArticle> mongoCollection;

        public ArticleController(ArticleService articleService)
        {
            ArticleService = articleService;
            mongoCollection = articleService.mongoCollection;
        }

        [HttpGet("GetFromUrl")]
        public async Task<IActionResult> GetData()
        {
            var data = await ArticleService.GetFromUrl();
            return Ok(data);
        }

        [HttpGet("Save")]
        public async Task<IActionResult> SaveMongoDb()
        {
            await ArticleService.Save();
            return Ok();
        }

        [HttpGet("Show")]
        public async Task<IActionResult> Show()
        {
            var result = await mongoCollection.Find(_ => true).ToListAsync();
            Console.WriteLine($"Fetched {result.Count} articles.");
            return Ok(result);
        }
    }
}
