using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;
using System.Text.Json;

namespace MovieTicketWebApi.Service
{
    public class StorageMovieTmdb
    {
        public readonly HttpClient _httpClient;
        public readonly IMongoCollection<MoviesInfomation> _movies;
        public StorageMovieTmdb(HttpClient http,MongoDbContext dbContext)
        {

            _httpClient = http;
            _movies = dbContext.Storage;
            
        }
        public async Task<List<MoviesInfomation>> Get()
        {

            var allMovies = new List<MoviesInfomation>();
            var apikey = "f0ab50cc5acff8fa95bb6bda373e8aa9";
            var url = $"https://api.themoviedb.org/3/movie/now_playing?api_key={apikey}&language=en-US&page=1";
            HttpResponseMessage response1=null;
            {
                for (int i = 1; i < 30; i++)
                {
                   response1 = await _httpClient.GetAsync($"https://api.themoviedb.org/3/movie/now_playing?api_key={apikey}&language=en-US&page={i}");
                    if (response1 != null && response1.IsSuccessStatusCode)
                    {
                        var jsonString = await response1.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<MovieApiResponse>(jsonString);
                        if (result != null)
                        {
                            allMovies.AddRange(result.Results);
                        }
                    }
            }
            
            }

            return allMovies;
 
        }
        public async Task SaveToMongoDb()
        {
            var movie=await Get();
            await _movies.DeleteManyAsync(Builders<MoviesInfomation>.Filter.Empty);
            var uniqueMovies = movie.GroupBy(m => m.Id).Select(g => g.First()).ToList();
            await _movies.InsertManyAsync(uniqueMovies);


        }
        public async Task<List<MoviesInfomation>> Show()
        {
            return await _movies.Find(_ => true).ToListAsync();
        }
    
}
}
