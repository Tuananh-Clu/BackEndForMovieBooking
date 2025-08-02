namespace MovieTicketWebApi.Model.Cinema
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string CollectionName { get; set; }
        public string CollectionTmdb { get; set; }
    }

}
