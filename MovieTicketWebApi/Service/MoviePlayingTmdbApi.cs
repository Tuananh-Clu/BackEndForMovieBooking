using MongoDB.Driver;
using MovieTicketWebApi.Model.Cinema;
using System.Text.Json;

namespace MovieTicketWebApi.Service
{
    public class MoviePlayingTmdbApi
    {
        public readonly HttpClient httpClient;
        public readonly IMongoCollection<MoviesInfomation> mongoCollection;
        public MoviePlayingTmdbApi(HttpClient http,IMongoClient client)
        {
            httpClient = http;
            var database = client.GetDatabase("NowPlayingTmdb");
            mongoCollection = database.GetCollection<MoviesInfomation>("Movies");
        }
        public async Task<List<MoviesInfomation>> getAll()
        {
            var apikey= "f0ab50cc5acff8fa95bb6bda373e8aa9";
            var url= $"https://api.themoviedb.org/3/movie/now_playing?api_key={apikey}&language=en-US&page=1";
            var response= await httpClient.GetAsync(url);
            var jsonString= await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MovieApiResponse>(jsonString);
            return result.Results;
        }
        public async Task SaveAllToMongoDb()
        {
            var movies=await getAll();
            await mongoCollection.DeleteManyAsync(Builders<MoviesInfomation>.Filter.Empty);
            await mongoCollection.InsertManyAsync(movies);
        }
        public async Task<List<MoviesInfomation>> Show()
        {
            return await mongoCollection.Find(_ => true).ToListAsync();
        }
    }
}
