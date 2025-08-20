using Amazon.Auth.AccessControlPolicy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Service;
using MovieTicketWebApi.Service.Article;

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
        policy.WithOrigins(
            "https://ap-cinema.vercel.app",
            "http://localhost:5173",        
            "http://localhost:3000"        
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
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
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = "https://teaching-squirrel-85.clerk.accounts.dev";


    options.Audience = "https://localhost:7083";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
       
    };
});

builder.Services.AddAuthorization();



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MovieTicket API v1");
});


app.UseCors("AllowFrontend");

app.UseRouting();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        Console.WriteLine($"=== REQUEST TO {context.Request.Path} ===");
        foreach (var header in context.Request.Headers)
        {
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
        Console.WriteLine("=== END HEADERS ===");
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
