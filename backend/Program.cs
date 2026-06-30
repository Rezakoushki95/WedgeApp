using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// appsettings.json is optional: `import` and the web app need no API key;
// only `fetch` requires AlphaVantage:ApiKey and will fail clearly if absent.
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=WedgeApp.db"));

builder.Services.AddHttpClient();
builder.Services.AddScoped<backend.Services.ChartService>();
builder.Services.AddScoped<backend.Services.ScoringService>();
builder.Services.AddScoped<backend.Services.JourneyService>();

var cacheDir = Path.Combine(builder.Environment.ContentRootPath, "data", "raw");

var app = builder.Build();

// CLI command mode: `dotnet run -- fetch SPY 2014 2024` / `dotnet run -- import`
if (args.Length > 0)
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    switch (args[0])
    {
        case "fetch":
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: dotnet run -- fetch <SYMBOL> <startYear> <endYear>");
                return;
            }
            var cfg = sp.GetRequiredService<IConfiguration>();
            var apiKey = cfg["AlphaVantage:ApiKey"]
                ?? throw new InvalidOperationException("AlphaVantage:ApiKey not set (appsettings.json).");
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var fetcher = new MarketDataFetcher(http, cacheDir, apiKey);
            var r = await fetcher.FetchRangeAsync(args[1], int.Parse(args[2]), int.Parse(args[3]));
            Console.WriteLine(
                $"fetched {r.Fetched}, skipped {r.Skipped}, " +
                $"{(r.CapReached ? "daily cap reached" : r.RangeComplete ? "range complete" : "stopped early")}");
            return;
        }
        case "import":
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            var importer = new MarketDataImporter(db, cacheDir);
            var r = await importer.ImportAllAsync();
            Console.WriteLine(
                $"imported {r.MonthsImported} months / {r.BarsImported} bars " +
                $"(processed {r.FilesProcessed}, skipped {r.FilesSkipped})");
            return;
        }
        default:
            Console.WriteLine($"Unknown command '{args[0]}'. Commands: fetch, import.");
            return;
    }
}

// Web startup: ensure schema, then import from cache if the DB has no market data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!await db.MarketDataMonths.AnyAsync())
    {
        var r = await new MarketDataImporter(db, cacheDir).ImportAllAsync();
        Console.WriteLine($"Startup import: {r.MonthsImported} months / {r.BarsImported} bars from cache.");
    }
}

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
