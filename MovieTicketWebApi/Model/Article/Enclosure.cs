using MongoDB.Bson.Serialization.Attributes;

namespace MovieTicketWebApi.Model.Article
{
    public class Enclosure
    {
        [BsonElement("link")]
        public string Link { get; set; }

        [BsonElement("type")]
        public string Type { get; set; }
        [BsonElement("lenght")]
        public int Length { get; set; }
    }
}
