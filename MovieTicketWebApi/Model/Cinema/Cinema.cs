namespace MovieTicketWebApi.Model.Cinema
{
    public class Cinema
    {
        public string id { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string image { get; set; }
        public List<Rooms> rooms { get; set; }
        public string city { get; set; }
        public string brand { get; set; }
        public string phone { get; set; }
        public List<string> showtimes { get; set; };
        public bool showtimes_available { get; set; }
        public List<string> current_movies { get; set; }
        public List<string> upcoming { get; set; }
    }
}
