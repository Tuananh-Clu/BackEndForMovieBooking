namespace MovieTicketWebApi.Model.Cinema
{
    public class DaySelect
    {
        public string Date { get; set; }
        public string CinemaName { get; set; }
        public string CinemaId { get; set; }
        public List<string> time {  get; set; }
        public string RoomName { get; set; }
        public string RoomId { get; set; }
        public string Location { get; set; }
        public string MovieTitle { get; set; }
        public string Poster { get; set; }
    }
}
