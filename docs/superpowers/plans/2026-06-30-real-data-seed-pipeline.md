# Real-Data Seed Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fetch real SPY 5-min RTH bars once into a durable gitignored on-disk cache, and rebuild the SQLite DB from that cache offline — so a fresh clone runs with no API key or network.

**Architecture:** Split the old `MarketDataService` into two single-purpose units — `MarketDataFetcher` (network → verbatim raw JSON files) and `MarketDataImporter` (raw JSON files → SQLite, offline + idempotent) — driven by a CLI command mode on the backend project. Web startup imports from the cache instead of fetching.

**Tech Stack:** .NET 8 / C# Web API, EF Core 8 + SQLite, Newtonsoft.Json (already referenced), xUnit for tests.

## Global Constraints

- Target framework: `net8.0` (SDK 8.0.419 installed).
- JSON parsing uses **Newtonsoft.Json** (`[JsonProperty(...)]`), matching `MarketDataResponse` / `BarData`.
- Prices are stored as **real, un-scaled** `decimal` values — the old `Open*10` scaling is removed everywhere.
- Alpha Vantage URL params are fixed: `function=TIME_SERIES_INTRADAY`, `interval=5min`, `adjusted=false`, `extended_hours=false`, `outputsize=full`, `month=yyyy-MM`.
- Raw cache lives at `backend/data/raw/` and is gitignored. Cache filenames are `{SYMBOL}-{yyyy-MM}.json`.
- Free-tier limits the fetcher must respect: ≤5 requests/min, stop after 25 successful fetches per run.
- New services live in namespace `backend.Services`. Models are in `backend.Models`; DbContext is `backend.Data.AppDbContext`.

---

## File Structure

**Create:**
- `backend/Services/MarketDataImporter.cs` — reads `data/raw/*.json` → SQLite; owns normalization + idempotency. Declares `ImportResult`.
- `backend/Services/MarketDataFetcher.cs` — Alpha Vantage → verbatim cache files; rate-limit/cap aware. Declares `FetchResult`.
- `backend.Tests/backend.Tests.csproj` — xUnit test project.
- `backend.Tests/MarketDataImporterTests.cs`
- `backend.Tests/MarketDataFetcherTests.cs`
- `backend.Tests/StubHttpMessageHandler.cs` — test helper for faking HTTP.
- `backend.Tests/Fixtures/spy-sample.json` — trimmed real-shaped AV response.

**Modify:**
- `backend/Program.cs` — add CLI dispatch (`fetch`/`import`), make `appsettings.json` optional, replace startup network-fetch with cache-import.
- `.gitignore` — add `/backend/data/`.

**Delete:**
- `backend/Services/MarketDataService.cs`
- `backend/Controllers/MarketDataController.cs`

---

## Task 1: Test project scaffold

**Files:**
- Create: `backend.Tests/backend.Tests.csproj` (via template)
- Create: `backend.Tests/ScaffoldSanityTest.cs`
- Modify: `WedgeApp.sln`

**Interfaces:**
- Consumes: nothing.
- Produces: a buildable xUnit project referencing `backend.csproj`, runnable via `dotnet test`.

- [ ] **Step 1: Generate the xUnit project**

Run from repo root:
```bash
dotnet new xunit -o backend.Tests
```
Expected: creates `backend.Tests/backend.Tests.csproj` and a `UnitTest1.cs`.

- [ ] **Step 2: Delete the template test, add a sanity test**

Delete `backend.Tests/UnitTest1.cs`. Create `backend.Tests/ScaffoldSanityTest.cs`:
```csharp
namespace backend.Tests;

public class ScaffoldSanityTest
{
    [Fact]
    public void Scaffold_Builds_And_Runs()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 3: Reference the backend project and add to the solution**

Run from repo root:
```bash
dotnet add backend.Tests/backend.Tests.csproj reference backend/backend.csproj
dotnet sln WedgeApp.sln add backend.Tests/backend.Tests.csproj
```
Expected: `Reference ... added` and `Project ... added to the solution`.

- [ ] **Step 4: Run the test**

Run from repo root:
```bash
dotnet test backend.Tests/backend.Tests.csproj
```
Expected: PASS — `Passed!  - Failed: 0, Passed: 1`.

- [ ] **Step 5: Commit**

```bash
git add backend.Tests WedgeApp.sln
git commit -m "test: scaffold xUnit test project for backend"
```

---

## Task 2: MarketDataImporter (raw cache → SQLite)

**Files:**
- Create: `backend/Services/MarketDataImporter.cs`
- Create: `backend.Tests/Fixtures/spy-sample.json`
- Create: `backend.Tests/MarketDataImporterTests.cs`
- Modify: `backend.Tests/backend.Tests.csproj` (copy fixture to output)

**Interfaces:**
- Consumes: `backend.Data.AppDbContext`, `backend.Models.{MarketDataMonth,MarketDataDay,FiveMinuteBar}`, `MarketDataResponse`/`BarData` (Newtonsoft).
- Produces:
  - `class backend.Services.MarketDataImporter` with ctor `(AppDbContext db, string cacheDir)`.
  - `Task<ImportResult> ImportAllAsync()`.
  - `record ImportResult(int FilesProcessed, int FilesSkipped, int MonthsImported, int BarsImported)`.
  - Cache filename contract: `{SYMBOL}-{yyyy-MM}.json`; the `yyyy-MM` segment determines the `MarketDataMonth.Month` (day = 1) and is the idempotency key.

- [ ] **Step 1: Add the fixture file**

Create `backend.Tests/Fixtures/spy-sample.json` (4 bars across 2 days, real prices):
```json
{
  "Meta Data": {
    "1. Information": "Intraday (5min) open, high, low, close prices and volume",
    "2. Symbol": "SPY",
    "4. Interval": "5min"
  },
  "Time Series (5min)": {
    "2020-03-02 09:30:00": { "1. open": "297.26", "2. high": "298.10", "3. low": "297.00", "4. close": "297.95", "5. volume": "1000" },
    "2020-03-02 09:35:00": { "1. open": "297.95", "2. high": "298.50", "3. low": "297.80", "4. close": "298.40", "5. volume": "1200" },
    "2020-03-03 09:30:00": { "1. open": "300.10", "2. high": "300.90", "3. low": "299.50", "4. close": "299.80", "5. volume": "1500" },
    "2020-03-03 09:35:00": { "1. open": "299.80", "2. high": "300.20", "3. low": "299.10", "4. close": "299.30", "5. volume": "1100" }
  }
}
```

- [ ] **Step 2: Make the fixture copy to test output**

In `backend.Tests/backend.Tests.csproj`, add inside a new `<ItemGroup>`:
```xml
  <ItemGroup>
    <None Update="Fixtures\spy-sample.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

- [ ] **Step 3: Write the failing importer tests**

Create `backend.Tests/MarketDataImporterTests.cs`:
```csharp
using backend.Data;
using backend.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests;

public class MarketDataImporterTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly string _cacheDir;

    public MarketDataImporterTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();

        _cacheDir = Path.Combine(Path.GetTempPath(), "wedge-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_cacheDir);
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "Fixtures", "spy-sample.json"),
            Path.Combine(_cacheDir, "SPY-2020-03.json"));
    }

    public void Dispose()
    {
        _conn.Dispose();
        if (Directory.Exists(_cacheDir)) Directory.Delete(_cacheDir, true);
    }

    [Fact]
    public async Task ImportAllAsync_LoadsMonthDaysAndBars_WithRealPrices()
    {
        using var ctx = new AppDbContext(_options);
        var importer = new MarketDataImporter(ctx, _cacheDir);

        var result = await importer.ImportAllAsync();

        Assert.Equal(1, result.MonthsImported);
        Assert.Equal(4, result.BarsImported);

        var month = await ctx.MarketDataMonths.SingleAsync();
        Assert.Equal(2020, month.Month.Year);
        Assert.Equal(3, month.Month.Month);

        Assert.Equal(2, await ctx.MarketDataDays.CountAsync());

        var firstBar = await ctx.FiveMinuteBars
            .OrderBy(b => b.Timestamp).FirstAsync();
        Assert.Equal(297.26m, firstBar.Open);   // real price, NOT *10
        Assert.Equal(298.10m, firstBar.High);
    }

    [Fact]
    public async Task ImportAllAsync_IsIdempotent()
    {
        using (var ctx = new AppDbContext(_options))
            await new MarketDataImporter(ctx, _cacheDir).ImportAllAsync();

        using var ctx2 = new AppDbContext(_options);
        var second = await new MarketDataImporter(ctx2, _cacheDir).ImportAllAsync();

        Assert.Equal(0, second.MonthsImported);
        Assert.Equal(1, second.FilesSkipped);
        Assert.Equal(1, await ctx2.MarketDataMonths.CountAsync());
        Assert.Equal(4, await ctx2.FiveMinuteBars.CountAsync());
    }
}
```

- [ ] **Step 4: Run the tests to verify they fail**

Run:
```bash
dotnet test backend.Tests/backend.Tests.csproj --filter MarketDataImporterTests
```
Expected: FAIL — does not compile (`MarketDataImporter` / `ImportResult` not defined).

- [ ] **Step 5: Implement the importer**

Create `backend/Services/MarketDataImporter.cs`:
```csharp
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace backend.Services;

public record ImportResult(int FilesProcessed, int FilesSkipped, int MonthsImported, int BarsImported);

public class MarketDataImporter
{
    private readonly AppDbContext _db;
    private readonly string _cacheDir;

    public MarketDataImporter(AppDbContext db, string cacheDir)
    {
        _db = db;
        _cacheDir = cacheDir;
    }

    public async Task<ImportResult> ImportAllAsync()
    {
        if (!Directory.Exists(_cacheDir))
            return new ImportResult(0, 0, 0, 0);

        int processed = 0, skipped = 0, months = 0, bars = 0;

        foreach (var path in Directory.GetFiles(_cacheDir, "*.json").OrderBy(p => p))
        {
            processed++;

            if (!TryParseMonthFromFileName(Path.GetFileName(path), out var targetMonth))
            {
                Console.WriteLine($"Skipping unrecognized file name: {Path.GetFileName(path)}");
                skipped++;
                continue;
            }

            if (await _db.MarketDataMonths
                    .AnyAsync(m => m.Month.Year == targetMonth.Year && m.Month.Month == targetMonth.Month))
            {
                skipped++;
                continue;
            }

            MarketDataResponse? parsed;
            try
            {
                parsed = JsonConvert.DeserializeObject<MarketDataResponse>(await File.ReadAllTextAsync(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping unparseable file {Path.GetFileName(path)}: {ex.Message}");
                skipped++;
                continue;
            }

            if (parsed?.TimeSeries == null || parsed.TimeSeries.Count == 0)
            {
                Console.WriteLine($"Skipping empty file: {Path.GetFileName(path)}");
                skipped++;
                continue;
            }

            var month = new MarketDataMonth { Month = targetMonth };
            _db.MarketDataMonths.Add(month);
            await _db.SaveChangesAsync();

            var byDay = parsed.TimeSeries
                .OrderBy(e => DateTime.Parse(e.Key))
                .GroupBy(e => DateTime.Parse(e.Key).Date);

            foreach (var dayGroup in byDay.OrderBy(g => g.Key))
            {
                var day = new MarketDataDay
                {
                    Date = dayGroup.Key,
                    MarketDataMonthId = month.Id,
                    FiveMinuteBars = dayGroup.Select(e => new FiveMinuteBar
                    {
                        Timestamp = DateTime.Parse(e.Key),
                        Open = e.Value.Open,
                        High = e.Value.High,
                        Low = e.Value.Low,
                        Close = e.Value.Close
                    }).ToList()
                };
                _db.MarketDataDays.Add(day);
                bars += day.FiveMinuteBars.Count;
            }

            await _db.SaveChangesAsync();
            months++;
        }

        return new ImportResult(processed, skipped, months, bars);
    }

    private static bool TryParseMonthFromFileName(string fileName, out DateTime month)
    {
        // Expected: {SYMBOL}-{yyyy-MM}.json  e.g. SPY-2020-03.json
        month = default;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var dash = name.IndexOf('-');
        if (dash < 0 || dash + 1 >= name.Length) return false;
        var datePart = name[(dash + 1)..]; // yyyy-MM
        return DateTime.TryParse(datePart + "-01", out month);
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run:
```bash
dotnet test backend.Tests/backend.Tests.csproj --filter MarketDataImporterTests
```
Expected: PASS — `Failed: 0, Passed: 2`.

- [ ] **Step 7: Commit**

```bash
git add backend/Services/MarketDataImporter.cs backend.Tests
git commit -m "feat: MarketDataImporter loads raw cache into SQLite with real prices"
```

---

## Task 3: MarketDataFetcher (Alpha Vantage → raw cache)

**Files:**
- Create: `backend/Services/MarketDataFetcher.cs`
- Create: `backend.Tests/StubHttpMessageHandler.cs`
- Create: `backend.Tests/MarketDataFetcherTests.cs`

**Interfaces:**
- Consumes: `System.Net.Http.HttpClient`.
- Produces:
  - `class backend.Services.MarketDataFetcher` with ctor `(HttpClient http, string cacheDir, string apiKey, int dailyCap = 25, Func<TimeSpan, Task>? delay = null)`.
  - `Task<FetchResult> FetchRangeAsync(string symbol, int startYear, int endYear)`.
  - `record FetchResult(int Fetched, int Skipped, bool CapReached, bool RangeComplete)`.
  - Writes verbatim response bodies to `{cacheDir}/{SYMBOL}-{yyyy-MM}.json`; never overwrites an existing file; stops without writing on an invalid/rate-limited response.

- [ ] **Step 1: Add the stub HTTP handler helper**

Create `backend.Tests/StubHttpMessageHandler.cs`:
```csharp
using System.Net;

namespace backend.Tests;

public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public int CallCount { get; private set; }

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_responder(request));
    }

    public static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };
}
```

- [ ] **Step 2: Write the failing fetcher tests**

Create `backend.Tests/MarketDataFetcherTests.cs`:
```csharp
using backend.Services;

namespace backend.Tests;

public class MarketDataFetcherTests : IDisposable
{
    private readonly string _cacheDir;

    private const string ValidBody =
        "{\"Time Series (5min)\":{\"2020-01-02 09:30:00\":" +
        "{\"1. open\":\"100.0\",\"2. high\":\"101.0\",\"3. low\":\"99.0\",\"4. close\":\"100.5\"}}}";

    public MarketDataFetcherTests()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), "wedge-fetch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_cacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDir)) Directory.Delete(_cacheDir, true);
    }

    [Fact]
    public async Task FetchRangeAsync_SkipsMonthsAlreadyCached()
    {
        // Pre-seed one month so it should be skipped (handler not called for it).
        File.WriteAllText(Path.Combine(_cacheDir, "SPY-2020-01.json"), ValidBody);

        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(ValidBody));
        var fetcher = new MarketDataFetcher(
            new HttpClient(handler), _cacheDir, "KEY", dailyCap: 25, delay: _ => Task.CompletedTask);

        var result = await fetcher.FetchRangeAsync("SPY", 2020, 2020); // 12 months

        Assert.Equal(11, result.Fetched);          // 12 months - 1 pre-cached
        Assert.Equal(1, result.Skipped);
        Assert.True(result.RangeComplete);
        Assert.Equal(11, handler.CallCount);       // never called for the cached month
        Assert.Equal(12, Directory.GetFiles(_cacheDir, "*.json").Length);
    }

    [Fact]
    public async Task FetchRangeAsync_StopsAtDailyCap()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(ValidBody));
        var fetcher = new MarketDataFetcher(
            new HttpClient(handler), _cacheDir, "KEY", dailyCap: 2, delay: _ => Task.CompletedTask);

        var result = await fetcher.FetchRangeAsync("SPY", 2020, 2020);

        Assert.Equal(2, result.Fetched);
        Assert.True(result.CapReached);
        Assert.False(result.RangeComplete);
        Assert.Equal(2, Directory.GetFiles(_cacheDir, "*.json").Length);
    }

    [Fact]
    public async Task FetchRangeAsync_StopsAndDoesNotWriteOnRateLimit()
    {
        var rateLimitBody = "{\"Information\":\"rate limit reached\"}";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(rateLimitBody));
        var fetcher = new MarketDataFetcher(
            new HttpClient(handler), _cacheDir, "KEY", dailyCap: 25, delay: _ => Task.CompletedTask);

        var result = await fetcher.FetchRangeAsync("SPY", 2020, 2020);

        Assert.Equal(0, result.Fetched);
        Assert.False(result.RangeComplete);
        Assert.Empty(Directory.GetFiles(_cacheDir, "*.json"));   // nothing written
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run:
```bash
dotnet test backend.Tests/backend.Tests.csproj --filter MarketDataFetcherTests
```
Expected: FAIL — does not compile (`MarketDataFetcher` / `FetchResult` not defined).

- [ ] **Step 4: Implement the fetcher**

Create `backend/Services/MarketDataFetcher.cs`:
```csharp
using Newtonsoft.Json.Linq;

namespace backend.Services;

public record FetchResult(int Fetched, int Skipped, bool CapReached, bool RangeComplete);

public class MarketDataFetcher
{
    private readonly HttpClient _http;
    private readonly string _cacheDir;
    private readonly string _apiKey;
    private readonly int _dailyCap;
    private readonly Func<TimeSpan, Task> _delay;

    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(12); // <=5/min

    private const string UrlTemplate =
        "https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={0}" +
        "&adjusted=false&interval=5min&month={1}&outputsize=full&extended_hours=false&apikey={2}";

    public MarketDataFetcher(HttpClient http, string cacheDir, string apiKey,
        int dailyCap = 25, Func<TimeSpan, Task>? delay = null)
    {
        _http = http;
        _cacheDir = cacheDir;
        _apiKey = apiKey;
        _dailyCap = dailyCap;
        _delay = delay ?? (ts => Task.Delay(ts));
    }

    public async Task<FetchResult> FetchRangeAsync(string symbol, int startYear, int endYear)
    {
        Directory.CreateDirectory(_cacheDir);

        int fetched = 0, skipped = 0;
        var cutoff = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        for (int year = startYear; year <= endYear; year++)
        {
            for (int m = 1; m <= 12; m++)
            {
                var month = new DateTime(year, m, 1);
                if (month > cutoff) return new FetchResult(fetched, skipped, false, true);

                var monthStr = month.ToString("yyyy-MM");
                var path = Path.Combine(_cacheDir, $"{symbol}-{monthStr}.json");

                if (File.Exists(path)) { skipped++; continue; }

                if (fetched >= _dailyCap)
                    return new FetchResult(fetched, skipped, true, false);

                if (fetched > 0) await _delay(ThrottleInterval);

                var url = string.Format(UrlTemplate, symbol, monthStr, _apiKey);
                string body;
                try
                {
                    body = await _http.GetStringAsync(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fetch error for {symbol} {monthStr}: {ex.Message}. Stopping.");
                    return new FetchResult(fetched, skipped, false, false);
                }

                if (!IsValidTimeSeries(body, out var reason))
                {
                    Console.WriteLine($"Stopping at {symbol} {monthStr}: {reason}");
                    return new FetchResult(fetched, skipped, false, false);
                }

                await File.WriteAllTextAsync(path, body); // verbatim
                fetched++;
                Console.WriteLine($"Fetched {symbol} {monthStr}");
            }
        }

        return new FetchResult(fetched, skipped, false, true);
    }

    private static bool IsValidTimeSeries(string body, out string reason)
    {
        reason = "";
        JObject obj;
        try { obj = JObject.Parse(body); }
        catch (Exception ex) { reason = "unparseable response: " + ex.Message; return false; }

        if (obj["Note"] != null) { reason = "rate-limit Note: " + obj["Note"]; return false; }
        if (obj["Information"] != null) { reason = "Information: " + obj["Information"]; return false; }

        var ts = obj["Time Series (5min)"] as JObject;
        if (ts == null || !ts.HasValues) { reason = "empty or missing Time Series (5min)"; return false; }
        return true;
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run:
```bash
dotnet test backend.Tests/backend.Tests.csproj --filter MarketDataFetcherTests
```
Expected: PASS — `Failed: 0, Passed: 3`.

- [ ] **Step 6: Commit**

```bash
git add backend/Services/MarketDataFetcher.cs backend.Tests
git commit -m "feat: MarketDataFetcher writes verbatim AV responses to raw cache, cap-aware"
```

---

## Task 4: Wire CLI + startup import, remove old fetch path

**Files:**
- Modify: `backend/Program.cs`
- Modify: `.gitignore`
- Delete: `backend/Services/MarketDataService.cs`
- Delete: `backend/Controllers/MarketDataController.cs`

**Interfaces:**
- Consumes: `backend.Services.MarketDataFetcher`, `backend.Services.MarketDataImporter`, `backend.Data.AppDbContext`.
- Produces: CLI commands `fetch <SYMBOL> <startYear> <endYear>` and `import`; web startup that imports from cache when the DB is empty and never touches the network.

- [ ] **Step 1: Delete the obsolete service and controller**

```bash
git rm backend/Services/MarketDataService.cs backend/Controllers/MarketDataController.cs
```

- [ ] **Step 2: Replace `backend/Program.cs`**

Overwrite `backend/Program.cs` with:
```csharp
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
```

- [ ] **Step 3: Gitignore the raw cache**

Append to `.gitignore`:
```
# Real market-data cache (fetched once, not redistributable)
/backend/data/
```

- [ ] **Step 4: Build the solution**

Run from repo root:
```bash
dotnet build WedgeApp.sln
```
Expected: `Build succeeded` with 0 errors (no remaining references to `MarketDataService`).

- [ ] **Step 5: Verify the import command runs with an empty cache**

Run from repo root:
```bash
dotnet run --project backend -- import
```
Expected: prints `imported 0 months / 0 bars (processed 0, skipped 0)` (no `data/raw/` yet) and exits 0 — no network call, no API key needed.

- [ ] **Step 6: Run the full test suite**

Run from repo root:
```bash
dotnet test WedgeApp.sln
```
Expected: PASS — all importer + fetcher + scaffold tests green (`Failed: 0`).

- [ ] **Step 7: Commit**

```bash
git add backend/Program.cs .gitignore
git commit -m "feat: CLI fetch/import commands + cache-based startup; remove runtime AV fetch"
```

---

## Manual verification (after Task 4, requires your real API key)

These are run by the user, not the agent — they spend real API budget.

1. Put your key in `backend/appsettings.json`:
   ```json
   { "AlphaVantage": { "ApiKey": "YOUR_KEY" } }
   ```
2. Fetch a small range to confirm the live path: `dotnet run --project backend -- fetch SPY 2023 2023`
   → expect up to 12 files in `backend/data/raw/` (or "daily cap reached" after 25 across runs).
3. `dotnet run --project backend -- import` → expect months/bars > 0.
4. `dotnet run --project backend` → Trade screen shows real SPY candles; no network at runtime.

## Self-Review Notes

- **Spec coverage:** Fetcher (network→cache, verbatim, ≤5/min via 12s throttle, 25/day cap, no-overwrite, stop-on-ratelimit) → Task 3. Importer (cache→DB, real prices, idempotent, skip bad files) → Task 2. CLI surface + optional appsettings + startup cache-import + removal of runtime fetch + endpoint removal → Task 4. Gitignore cache → Task 4. Tests → Tasks 2–3. SPY-only/parameterized scope → fetcher signature (Task 3) + manual verification. All spec sections mapped.
- **Type consistency:** `MarketDataImporter(AppDbContext, string)` / `ImportAllAsync()` / `ImportResult(FilesProcessed, FilesSkipped, MonthsImported, BarsImported)` and `MarketDataFetcher(HttpClient, string, string, int, Func<TimeSpan,Task>?)` / `FetchRangeAsync(string,int,int)` / `FetchResult(Fetched, Skipped, CapReached, RangeComplete)` are used identically in their tests and in Program.cs. Cache filename contract `{SYMBOL}-{yyyy-MM}.json` is consistent across fetcher (write), importer (parse), and tests.
- **Placeholders:** none — all code and commands are concrete.
