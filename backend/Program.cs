using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod() // Allows all HTTP methods
            .AllowAnyHeader());
});


// Register the DbContext with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=WedgeApp.db"));

// Register HttpClient for MarketDataService
builder.Services.AddHttpClient<MarketDataService>();

// Register MarketDataService
builder.Services.AddScoped<MarketDataService>();

builder.Services.AddScoped<TradingSessionService>();

builder.Services.AddScoped<UserStatsService>();

var app = builder.Build();

// Ensure initial data is available
var marketDataService = app.Services.CreateScope().ServiceProvider.GetRequiredService<MarketDataService>();
await marketDataService.EnsureInitialMonthlyData();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAllOrigins");
app.UseAuthorization();
app.MapControllers();
app.Run();

