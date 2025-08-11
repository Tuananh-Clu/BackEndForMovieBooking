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
            foreach (var ticket in tickets)
            {
                var filter = Builders<Cinema>.Filter.And(
                    Builders<Cinema>.Filter.Eq(a => a.address, ticket.Location),
                    Builders<Cinema>.Filter.ElemMatch(a => a.rooms, r => r.id == ticket.RoomId));
                var update=await mongoCollection.Find(filter).FirstOrDefaultAsync();
                if (update != null)
                {
                    var room = update.rooms.FirstOrDefault(r => r.id == ticket.RoomId);
                    if (room != null)
                    {
                        var showtime = room.showtimes.FirstOrDefault(s => s.date == ticket.Date && s.times.Contains(ticket.Time) && s.movie.title == ticket.MovieTitle);
                        if (showtime != null)
                        {
                            var seat = showtime.seats.FirstOrDefault(s => s.id == ticket.Id);
                            if (seat != null)
                            {
                                seat.isOrdered = "true";
                            }
                        }
                    }
                    await mongoCollection.ReplaceOneAsync(filter, update);
                }

            }
        }

        public async Task<List<Movie>> GetMovieBooking()
        {
            var cinemas = await mongoCollection.Find(_ => true).Project(s=>new { s.rooms }).ToListAsync();

            var allMovies = cinemas
                .SelectMany(c => c.rooms)
                .SelectMany(r => r.showtimes)
                .Select(s => s.movie)
                .GroupBy(m => m.id)
                .Select(g => g.First())
                .ToList();

            return allMovies;
        }
        public async Task<List<TheaterProp>> GetTheaterPropsAsync()
        {
            var theater=await mongoCollection.Find(_=>true).Project(s=>new TheaterProp
            {
                Id = s.id,
                Name = s.name,
                Address = s.address,
                Image = s.image,
                City=s.city

            }).ToListAsync();
            var theaterProps=theater.Select(theater => new TheaterProp
            {
                Id = theater.Id,
                Name = theater.Name,
                Address = theater.Address,
                Image = theater.Image,
                City = theater.City
            }).ToList();
            return theaterProps;
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
            var projection = Builders<Cinema>.Projection
                .Include(c => c.id)
                  .Include(c => c.name)
                   .Include(c => c.address)
                     .Include("rooms.id")
                         .Include("rooms.name")
                           .Include("rooms.showtimes");

            var filter = Builders<Cinema>.Filter.ElemMatch(
                c => c.rooms,
                r => r.showtimes.Any(s => s.movie.title == movieId)
            );

            var cinemas = await mongoCollection
                .Find(filter)
                .Project<Cinema>(projection)
                .ToListAsync();

            var daySelectList = cinemas
                .SelectMany(c => c.rooms
                    .SelectMany(r => r.showtimes
                        .Where(s => s.movie.title == movieId)
                        .Select(s => new DaySelect
                        {
                            Location = c.address,
                            Date = s.date,
                            CinemaName = c.name,
                            CinemaId = c.id,
                            time = s.times,
                            RoomName = r.name,
                            RoomId = r.id,
                            MovieTitle = s.movie.title,
                            Poster = s.movie.poster
                        })
                    )
                )
                .ToList();

            return daySelectList;
        }

        public async Task<List<TheaterInFo>> getInfoTheater(string location,string room,string movieTitle)
        {
            var filter = Builders<Cinema>.Filter.And(
                   Builders<Cinema>.Filter.Eq(c => c.address,location ),
                   Builders<Cinema>.Filter.ElemMatch(c => c.rooms, r => r.id == room)
               );
            var cinemas = await mongoCollection.Find(filter).ToListAsync();
            var result = cinemas.SelectMany(c => c.rooms.SelectMany(a => a.showtimes.Where(s => s.movie.title == movieTitle)
                .Select(d => new TheaterInFo
                {
                    Theateraddress = c.address,
                    Theatername = c.name,
                    Poster = d.movie.poster,
                    City=c.city

                }))).Distinct().ToList();
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
        public async Task<List<FullInfoTheater>> getTheaterBtId(string id)
        {
            var filter=Builders<Cinema>.Filter.Eq(c => c.id, id);
            var result=await mongoCollection.Find(filter).Project(c => new FullInfoTheater
            {
                Id = c.id,
                Name = c.name,
                Address = c.address,
                Image = c.image,
                City = c.city,
                Brand = c.brand,
                Phone = c.phone,
                Rooms = c.rooms
            }).ToListAsync();
            return result;
        }


    }
}