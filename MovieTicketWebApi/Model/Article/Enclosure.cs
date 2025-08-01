using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MovieTicketWebApi.Model.Article
{
    public class Enclosure
    {
        [BsonElement("link")]
        [JsonPropertyName("link")]
        public string Link { get; set; }

        [BsonElement("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [BsonElement("length")] 
        [JsonPropertyName("length")]
        public int Length { get; set; }
    }
}
