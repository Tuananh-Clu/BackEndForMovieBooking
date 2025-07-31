using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;
using System.Text.Json;

namespace MovieTicketWebApi.Service
{
    public class MovieUpcomingTmdbApi
    {
        public readonly HttpClient http;
        public readonly IMongoCollection<MoviesInfomation> mongoCollection;
        public MovieUpcomingTmdbApi(MongoDbContext dbContext,HttpClient https)
        {
            http = https;
            mongoCollection = dbContext.Upcoming;
        }
        public async Task<List<MoviesInfomation>> GetFromTmdb()
        {
            var apikey = "f0ab50cc5acff8fa95bb6bda373e8aa9";
            var url = $"https://api.themoviedb.org/3/movie/upcoming?api_key={apikey}&language=en-US&page=1";
            var response = await http.GetAsync(url);
            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<MovieApiResponse>(jsonString);
            return result.Results;
        }
        public async Task<List<MoviesInfomation>> ShowAll()
        {
             var data= await mongoCollection.Find(_ => true).ToListAsync();
            return data;
        }
        public async Task saveMongodb()
        {
            var data = await GetFromTmdb();
            mongoCollection.DeleteManyAsync(_=>true);
            mongoCollection.InsertManyAsync(data);
        }
    }
}
