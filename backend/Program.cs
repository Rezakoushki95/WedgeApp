using backend.Data;
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
builder.Services.AddScoped<MarketDataService>();
builder.Services.AddScoped<backend.Services.ChartService>();
builder.Services.AddScoped<backend.Services.ScoringService>();
builder.Services.AddScoped<backend.Services.JourneyService>();


var app = builder.Build();

// Ensure the database schema exists
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

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
