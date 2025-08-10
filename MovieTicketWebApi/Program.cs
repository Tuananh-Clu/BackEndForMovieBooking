using MongoDB.Driver;
using Microsoft.Extensions.Options;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Service;
using MovieTicketWebApi.Service.Article;
using MovieTicketWebApi.Model;

AppContext.SetSwitch("System.Net.Security.SslStream.UseLegacyTls", false);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MovieTicket API",
        Version = "v1"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://ap-cinema.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod().AllowCredentials();
    });
});

builder.Configuration.AddEnvironmentVariables();
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<ArticleService>();
builder.Services.AddSingleton<MovieUpcomingTmdbApi>();
builder.Services.AddSingleton<StorageMovieTmdb>();
builder.Services.AddSingleton<MoviePopularTmdbApi_cs>();
builder.Services.AddSingleton<MoviePlayingTmdbApi>();
builder.Services.AddSingleton<CinemaService>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Jwt:Issuer"];
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,
        };
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MovieTicket API v1");
});

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
