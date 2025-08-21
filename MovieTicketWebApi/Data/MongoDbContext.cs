using MongoDB.Driver;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Model.Article;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Model.User;
using System.Net.Sockets;

namespace MovieTicketWebApi.Data
{
    public class MongoDbContext
    {
        public readonly IMongoDatabase mongoDatabase;
        public MongoDbContext(IMongoClient client)
        {
            mongoDatabase = client.GetDatabase("MovieBooking");
        }
        public IMongoCollection<MoviesInfomation> NowPlayingMovies => mongoDatabase.GetCollection<MoviesInfomation>("NowPlayingMovies");
        public IMongoCollection<Cinema> Cinema => mongoDatabase.GetCollection<Cinema>("Cinemas");
        public IMongoCollection<MainArticle> Article => mongoDatabase.GetCollection<MainArticle>("Article");
        public IMongoCollection<MoviesInfomation> Popular => mongoDatabase.GetCollection<MoviesInfomation>("PopularMovies");
        public IMongoCollection<MoviesInfomation> Upcoming => mongoDatabase.GetCollection<MoviesInfomation>("UpcomingMovies");
        public IMongoCollection<MoviesInfomation> Storage => mongoDatabase.GetCollection<MoviesInfomation>("StorageMovies");
        public IMongoCollection<Client> User => mongoDatabase.GetCollection<Client>("User");
        public IMongoCollection<Client> Admin => mongoDatabase.GetCollection<Client>("Admin");
        public IMongoCollection<VoucherDb> Voucher=>mongoDatabase.GetCollection<VoucherDb>("Voucher");



    }
}
