using MovieTicketWebApi.Model.Cinema;
using System.Text.Json.Serialization;

public class MovieApiResponse
{
    [JsonPropertyName("results")]
    public List<MoviesInfomation> Results { get; set; }
}
