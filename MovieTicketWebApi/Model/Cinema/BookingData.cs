using MongoDB.Bson.Serialization.Attributes;

namespace MovieTicketWebApi.Model.Cinema
{
    public class BookingData
    {
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("rooms")]
        public List<Rooms> Rooms { get; set; }
    }
}
