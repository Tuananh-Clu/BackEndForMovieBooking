using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;
using System.Net.Http;
using System.Text.Json;

namespace MovieTicketWebApi.Service
{
    public class MoviePopularTmdbApi_cs
    {
        public readonly HttpClient _httpClient;
        public readonly IMongoCollection<MoviesInfomation> mongoCollection;
        public MoviePopularTmdbApi_cs(HttpClient http,MongoDbContext dbContext)
        {
            _httpClient = http;
            mongoCollection = dbContext.Popular;
        }
        public async Task<List<MoviesInfomation>> GetMovie()
        {
            var apikey = "f0ab50cc5acff8fa95bb6bda373e8aa9";
            var url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apikey}&language=en-US&page=1";
            var response = await _httpClient.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MovieApiResponse>(jsonString);
            return result.Results;
        }
        public async Task SaveAllMoviePopular()
        {
            var movies = await GetMovie();
            await mongoCollection.DeleteManyAsync(Builders<MoviesInfomation>.Filter.Empty);
            await mongoCollection.InsertManyAsync(movies);
        }
        public async Task<List<MoviesInfomation>> ShowAll()
        {
            return await mongoCollection.Find(_ => true).ToListAsync();
        }
    }
}
