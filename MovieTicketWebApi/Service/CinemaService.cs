using MongoDB.Bson;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Model.Ticket;

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
            return await mongoCollection.Find(_ => true).ToListAsync();
        }
        public async Task InsertAsync(List<Cinema> cinemas)
        {
            await mongoCollection.DeleteManyAsync(Builders<Cinema>.Filter.Empty);
            await mongoCollection.InsertManyAsync(cinemas);
        }
        public async Task Update(List<TicketInformation> tickets)
        {
            foreach (var ticket in tickets)
            {
                var cinema = await mongoCollection.Find(c => c.address == ticket.Location).FirstOrDefaultAsync();
                if (cinema == null) continue;

                var room = cinema.rooms.FirstOrDefault(r => r.name == ticket.RoomId);
                if (room == null) continue;

                var showtime = room.showtimes.FirstOrDefault(s =>
                    s.times.Contains(ticket.Time) &&
                    s.date == ticket.Date &&
                    s.movie.title == ticket.MovieTitle
                );
                if (showtime == null) continue;

                var seatIndex = room.seats.FindIndex(s => s.id == ticket.Id);
                if (seatIndex == -1) continue;

                room.seats[seatIndex].isOrdered = true;

                await mongoCollection.ReplaceOneAsync(c => c.id == cinema.id, cinema);
            }
        }

    }
}