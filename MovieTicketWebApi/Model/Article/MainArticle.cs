using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MovieTicketWebApi.Model.Article
{

    public class MainArticle
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("categories")]
        public List<string> Categories { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("enclosure")]
        public Enclosure Enclosure { get; set; }

        [BsonElement("guid")]
        public string Guid { get; set; }

        [BsonElement("link")]
        public string Link { get; set; }

        [BsonElement("pubDate")]
        public string PubDate { get; set; }

        [BsonElement("thumbnail")]
        public string Thumbnail { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }
    }
}
