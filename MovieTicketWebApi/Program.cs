using MongoDB.Driver;
using MovieTicketWebApi.Data;
using MovieTicketWebApi.Model.Cinema;
using MovieTicketWebApi.Service;
using MovieTicketWebApi.Service.Article;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ArticleService>();
builder.Services.AddSingleton<MovieUpcomingTmdbApi>();
builder.Services.AddSingleton<StorageMovieTmdb>();
builder.Services.AddSingleton<MoviePopularTmdbApi_cs>();
builder.Services.AddSingleton<MoviePlayingTmdbApi>();
builder.Services.AddSingleton<CinemaService>();
builder.Services.AddSingleton<MongoDbContext>();
AppContext.SetSwitch("System.Net.Security.SslStream.UseLegacyTls", false);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://ap-cinema.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
    return new MongoClient(settings.ConnectionString);
});

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

// Middleware order matters
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger(); // nếu muốn Swagger chạy ở cả production
    app.UseSwaggerUI();
}

app.UseRouting();              // ✅ BẮT BUỘC
app.UseCors("AllowFrontend"); // ✅ Trước Auth
app.UseAuthentication();      // ✅
app.UseAuthorization();       // ✅

app.MapControllers();

app.Run();
