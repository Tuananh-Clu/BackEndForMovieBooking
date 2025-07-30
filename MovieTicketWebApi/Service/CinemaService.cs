using MongoDB.Driver;
using MovieTicketWebApi.Model.Cinema;

namespace MovieTicketWebApi.Service
{
    public class CinemaService
    {
        public readonly IMongoCollection<Cinema> mongoCollection;
        public CinemaService(IMongoClient client)
        {
            var database = client.GetDatabase("CinemaDb");
            mongoCollection = database.GetCollection<Cinema>("Cinemas");
        }
        public async Task<List<Cinema>> GetCinemasAsync()
        {
            return await mongoCollection.Find(_=>true).ToListAsync();
        }
        public async Task InsertAsync(List<Cinema> cinemas)
        {
            await mongoCollection.DeleteManyAsync(Builders<Cinema>.Filter.Empty);
            await mongoCollection.InsertManyAsync(cinemas);
        }
    }
}
