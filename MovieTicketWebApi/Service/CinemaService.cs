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
        public async Task InsertAsync(List<Cinema> cinemas, [FromQuery] bool isFirst)
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
            var writes=new List<WriteModel<Cinema>>();
            foreach (var ticket in tickets)
            {
                var filter = Builders<Cinema>.Filter.Eq((a) => a.address, ticket.Location);
                var update = Builders<Cinema>.Update.Set("rooms.$[room].showtimes.$[showtime].seats.$[seat].isOrdered", "true");
                var arrayfilter = new[]
                {
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("room.id", ticket.RoomId)),
                    new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("seat.id", ticket.Id)),
                };
                var updatemodel = new UpdateManyModel<Cinema>(filter, update)
                {
                    ArrayFilters = arrayfilter
                };
                writes.Add(updatemodel);
                await mongoCollection.BulkWriteAsync(writes);   

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
            Console.WriteLine(cinemaId, roomId, newShowtime);

            await mongoCollection.UpdateOneAsync(filter, update);
        }
        public async Task<List<MovieTicketReport>> GetSoVeBanRa()
        {
            var cinema = await mongoCollection.Find(_ => true).ToListAsync();
            var DoanhThuBanRaTheoPhim = cinema.SelectMany(c => c.rooms).SelectMany(a => a.showtimes).
             GroupBy(z => new { z.movie.id, z.movie.poster, z.movie.title }).Select((group) => new MovieTicketReport
             {
                 title = group.Key.title,
                 Poster = group.Key.poster,
                 MovieId = group.Key.id,
                 count = group.SelectMany((c) => c.seats).Count(a => a.isOrdered == "true"),

             }).ToList();
            return DoanhThuBanRaTheoPhim;
        }
        public async Task<List<DaySelect>> GetNgayChieu(string movieId)
        {
            var filter = await mongoCollection.Find(_ => true).ToListAsync();
            var data = filter
            .SelectMany(cinema => cinema.rooms.SelectMany(room => room.showtimes.Select(showtime => new
            {
                CinemaName = cinema.name,
                CinemaId = cinema.id,
                RoomName = room.name,
                RoomId = room.id,
                Date = showtime.date,
                MovieTitle = showtime.movie.title,
                time = showtime.times,
                location=cinema.address,
                movieTittle=showtime.movie.title,
                poster=showtime.movie.poster,
            })))
     .Where(s => s.MovieTitle == movieId)
     .Select(s => new DaySelect
     {
         Location=s.location,
         Date = s.Date,
         CinemaName = s.CinemaName,
         CinemaId = s.CinemaId,
         time = s.time,
         RoomName = s.RoomName,
         RoomId = s.RoomId,
         MovieTitle= s.MovieTitle,
         Poster=s.poster,
         
     })
     .Distinct()
     .ToList();
            return data;
        }
    }
}