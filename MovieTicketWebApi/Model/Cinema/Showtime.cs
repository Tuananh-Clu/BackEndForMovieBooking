namespace MovieTicketWebApi.Model.Cinema
{
    public class Showtime
    {
        public string date { get; set; }
        public List<string> times { get; set; }
        public Movie movie { get; set; }
        public List<Seat> seats { get; set; }
    }
}
