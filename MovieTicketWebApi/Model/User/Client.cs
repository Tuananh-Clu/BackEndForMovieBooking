using MongoDB.Bson.Serialization.Attributes;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Model.Ticket;

namespace MovieTicketWebApi.Model.User
{
    public class Client
    {
        [BsonId]
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Avatar { get; set; }
        public string role { get; set; }
        public List<List<TicketInformation>> tickets { get; set; } = new();
        public List<Movie> YeuThich { get; set; } = new List<Movie>();
    }
}
