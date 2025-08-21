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
using MovieTicketWebApi.Service.Voucher;
using System.Net.Http;

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
builder.Services.AddSingleton<VoucherService>();
builder.Services.AddControllers();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://frank-bream-9.clerk.accounts.dev";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://frank-bream-9.clerk.accounts.dev",
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
        var jwksEndpoint = "https://frank-bream-9.clerk.accounts.dev/.well-known/jwks.json";
        var httpClient = new HttpClient();
        Microsoft.IdentityModel.Tokens.JsonWebKeySet? cachedJwks = null;
        DateTime cachedAt = DateTime.MinValue;
        TimeSpan cacheTtl = TimeSpan.FromMinutes(10);
        options.TokenValidationParameters.IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            try
            {
                if (cachedJwks == null || DateTime.UtcNow - cachedAt > cacheTtl)
                {
                    var jwksJson = httpClient.GetStringAsync(jwksEndpoint).GetAwaiter().GetResult();
                    cachedJwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);
                    cachedAt = DateTime.UtcNow;
                }
                var keys = cachedJwks?.Keys ?? new List<Microsoft.IdentityModel.Tokens.JsonWebKey>();
                if (!string.IsNullOrEmpty(kid))
                {
                    return keys.Where(k => string.Equals(k.Kid, kid, StringComparison.OrdinalIgnoreCase));
                }
                return keys;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"JWKS fetch/parse failed: {ex.Message}");
                return Array.Empty<Microsoft.IdentityModel.Tokens.SecurityKey>();
            }
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
          
                Console.WriteLine($"JWT challenge error: {context.Error}; description: {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
        options.RequireHttpsMetadata = true;
    });


builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MovieTicket API v1");
});


app.UseCors("AllowFrontend");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
