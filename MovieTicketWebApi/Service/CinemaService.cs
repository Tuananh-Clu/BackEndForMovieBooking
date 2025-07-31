using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;

namespace MovieTicketWebApi.Service
{
    public class CinemaService
    {
        public readonly IMongoCollection<Cinema> mongoCollection;
        public CinemaService(MongoDbContext dbContext)
        {
            mongoCollection = dbContext.Cinema;
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
