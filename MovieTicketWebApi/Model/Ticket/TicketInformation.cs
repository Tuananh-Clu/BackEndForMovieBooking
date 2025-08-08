using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MovieTicketWebApi.Model.Ticket
{
    public class TicketInformation
    {
        public string Id { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string Time { get; set; } = "";
        public string MovieTitle { get; set; } = "";
        public string Date { get; set; } = "";
        public int Price { get; set; } = 0;
        public int Quantity { get; set; } = 0;
        public string Image { get; set; } = "";
        public string SeatType { get; set; } = "";
        public string location { get; set; } = "";
        public string City { get; set; } = "";
        public string IsSelected { get; set; } = "true";
    }
}
