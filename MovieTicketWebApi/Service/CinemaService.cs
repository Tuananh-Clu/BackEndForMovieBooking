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
            var cinemas = await mongoCollection
                .Find(c => c.rooms.Any(r => r.showtimes.Any(s => s.movie.id == movieId)))
                .ToListAsync();

            var result = cinemas
                .SelectMany(c => c.rooms
                    .SelectMany(room => room.showtimes
                        .Where(showtime => showtime.movie.id == movieId)
                        .Select(showtime => new DaySelect
                        {
                            Location = c.address,
                            Date = showtime.date,
                            CinemaName = c.name,
                            CinemaId = c.id,
                            time = showtime.times,
                            RoomName = room.name,
                            RoomId = room.id,
                            MovieTitle = showtime.movie.title,
                            Poster = showtime.movie.poster
                        })
                    )
                )
                .Distinct()
                .ToList();

            return result;
        }



        public async Task<List<Seat>> Seats(string roomid, string title, string date, string time)
        {
            var cinema = await mongoCollection
                .Find(c => c.rooms.Any(r => r.id == roomid
                    && r.showtimes.Any(s =>
                        s.movie.title == title &&
                        s.date == date &&
                        s.times.Contains(time)
                    )
                ))
                .FirstOrDefaultAsync();

            if (cinema == null)
                return new List<Seat>();

            return cinema.rooms
                .Where(r => r.id == roomid)
                .SelectMany(r => r.showtimes)
                .Where(s =>
                    s.movie.title == title &&
                    s.date == date &&
                    s.times.Contains(time)
                )
                .SelectMany(s => s.seats)
                .ToList();
        }


    }
}