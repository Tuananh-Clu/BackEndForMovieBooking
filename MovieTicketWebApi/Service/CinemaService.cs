using Microsoft.AspNetCore.Mvc;
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
        public async Task InsertAsync(List<Cinema> cinemas, [FromQuery]bool isFirst)
        {
            try
            {
                if (isFirst)
                {
                    await mongoCollection.DeleteManyAsync(_ => true);
                }
                else
                {
                    await mongoCollection.InsertManyAsync(cinemas);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi InsertAsync: " + ex.Message);
                throw;
            }
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

                var seatIndex = showtime.seats.FindIndex(s => s.id == ticket.Id);
                if (seatIndex == -1) continue;

                showtime.seats[seatIndex].isOrdered = "true";

                await mongoCollection.ReplaceOneAsync(c => c.id == cinema.id, cinema);
            }
        }
        public async Task<List<Movie>> GetMovieBooking()
        {
            var cinemas = await mongoCollection.Find(_ => true).ToListAsync();

            var allMovies = cinemas
                .SelectMany(c => c.rooms)
                .SelectMany(r => r.showtimes)
                .Select(s => s.movie)
                .GroupBy(m => m.id)
                .Select(g => g.First())
                .ToList();

            return allMovies;
        }
        public async Task AddShowtimeAsync(string cinemaId, string roomId, Showtime newShowtime)
        {
            var filter = Builders<Cinema>.Filter.And(
                Builders<Cinema>.Filter.Eq(c => c.name, cinemaId),
                Builders<Cinema>.Filter.ElemMatch(c => c.rooms, r => r.id == roomId)
            );

            var update = Builders<Cinema>.Update.Push("rooms.$.showtimes", newShowtime);
            Console.WriteLine(cinemaId,roomId, newShowtime);

            await mongoCollection.UpdateOneAsync(filter, update);
        }
        public async Task<List<MovieTicketReport>> GetSoVeBanRa()
        {
            var cinema=await mongoCollection.Find(_ => true).ToListAsync();
            var DoanhThuBanRaTheoPhim = cinema.SelectMany(c => c.rooms).SelectMany(a => a.showtimes).
             GroupBy(z => new {z.movie.id,z.movie.poster,z.movie.title}).Select((group) => new MovieTicketReport
             {
                 title=group.Key.title,
                 Poster=group.Key.poster,
                 MovieId = group.Key.id,
                 count = group.SelectMany((c) => c.seats).Count(a => a.isOrdered=="true"),

             }).ToList();
            return DoanhThuBanRaTheoPhim;
        }

    }
}