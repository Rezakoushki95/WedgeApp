using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
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

// Register services
builder.Services.AddHttpClient<MarketDataService>(); // HttpClient for API calls
builder.Services.AddScoped<MarketDataService>(); // Market data fetching and storage logic
builder.Services.AddScoped<TradingSessionService>(); // Trading session logic
builder.Services.AddScoped<UserStatsService>(); // User stats updating logic
builder.Services.AddScoped<AccessManagementService>(); // Access management logic

var app = builder.Build();

// Ensure initial market data
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
