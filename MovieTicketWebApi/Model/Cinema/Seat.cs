using System.Text.Json.Serialization;

namespace MovieTicketWebApi.Model.Cinema
{
    public class Seat
    {
        public string id { get; set; }
        public string isOrdered { get; set; }
        [JsonPropertyName("price")]
        public decimal price { get; set; }
        [JsonPropertyName("type")]
        public string type { get; set; }
    }
}
