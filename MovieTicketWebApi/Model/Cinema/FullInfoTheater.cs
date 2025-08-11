namespace MovieTicketWebApi.Model.Cinema
{
    public class FullInfoTheater
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Image { get; set; }
        public string City { get; set; }
        public List<Rooms> Rooms { get; set; } = new List<Rooms>();
        public string Brand { get; set; }
        public string Phone { get; set; }
    }
}
