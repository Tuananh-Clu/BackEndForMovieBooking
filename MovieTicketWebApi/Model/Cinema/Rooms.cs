using MovieTicketWebApi.Model.Cinema;

namespace MovieTicketWebApi.Model
{
    public class Rooms
    {
        public string id { get; set; }
        public string name { get; set; }
        public List<Showtime> showtimes { get; set; }

    }
}
