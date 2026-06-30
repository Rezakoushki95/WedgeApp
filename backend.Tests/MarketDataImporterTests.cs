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

    [Fact]
    public async Task ImportAllAsync_SkipsMalformedFile_WithoutThrowing()
    {
        // Isolated temp dir with only a bad file — no fixture interference.
        var tempDir = Path.Combine(Path.GetTempPath(), "wedge-test-malformed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "SPY-2021-01.json"), "{ not valid json");

            using var ctx = new AppDbContext(_options);
            var importer = new MarketDataImporter(ctx, tempDir);

            var result = await importer.ImportAllAsync();

            Assert.Equal(0, result.MonthsImported);
            Assert.True(result.FilesSkipped >= 1);
            Assert.Equal(0, await ctx.MarketDataMonths.CountAsync());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ImportAllAsync_ImportsTwoMonths()
    {
        // Isolated temp dir with two valid month files — exercises the multi-month loop
        // and Fix 1's per-month ChangeTracker.Clear().
        var tempDir = Path.Combine(Path.GetTempPath(), "wedge-test-twomonths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "spy-sample.json"),
                Path.Combine(tempDir, "SPY-2020-03.json"));

            // Second month with a small valid payload (2 bars, one day)
            const string april2020Json = """
                {
                  "Meta Data": { "2. Symbol": "SPY" },
                  "Time Series (5min)": {
                    "2020-04-01 09:30:00": { "1. open": "280.00", "2. high": "281.00", "3. low": "279.00", "4. close": "280.50", "5. volume": "1000" },
                    "2020-04-01 09:35:00": { "1. open": "280.50", "2. high": "281.50", "3. low": "280.00", "4. close": "281.00", "5. volume": "1100" }
                  }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(tempDir, "SPY-2020-04.json"), april2020Json);

            using var ctx = new AppDbContext(_options);
            var importer = new MarketDataImporter(ctx, tempDir);

            var result = await importer.ImportAllAsync();

            Assert.Equal(2, result.MonthsImported);
            Assert.Equal(2, await ctx.MarketDataMonths.CountAsync());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ImportAllAsync_TwoSymbolsSameMonth_SkipsSecondSymbol()
    {
        // Two symbols for the same month in the same cache dir.
        // Files are processed in sorted order: QQQ-2020-03.json < SPY-2020-03.json (Q < S),
        // so QQQ becomes the canonical symbol and SPY is skipped with a warning.
        // This prevents silent month collisions without adding a Symbol column.
        var tempDir = Path.Combine(Path.GetTempPath(), "wedge-test-multisym-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // QQQ processed FIRST (alphabetical order): sets canonical symbol = QQQ.
            // Distinct first-bar open (200.00m) lets us verify which data was actually imported.
            const string qqqJson = """
                {
                  "Meta Data": { "2. Symbol": "QQQ" },
                  "Time Series (5min)": {
                    "2020-03-02 09:30:00": { "1. open": "200.00", "2. high": "201.00", "3. low": "199.00", "4. close": "200.50", "5. volume": "1000" }
                  }
                }
                """;
            await File.WriteAllTextAsync(Path.Combine(tempDir, "QQQ-2020-03.json"), qqqJson);

            // SPY processed SECOND: guard detects a second symbol and skips it.
            // SPY's first bar open is 297.26m — must NOT appear in the imported data.
            File.Copy(
                Path.Combine(AppContext.BaseDirectory, "Fixtures", "spy-sample.json"),
                Path.Combine(tempDir, "SPY-2020-03.json"));

            using var ctx = new AppDbContext(_options);
            var importer = new MarketDataImporter(ctx, tempDir);

            var result = await importer.ImportAllAsync();

            // Only the alphabetically-first symbol (QQQ) is imported; SPY is skipped
            Assert.Equal(1, result.MonthsImported);
            Assert.True(result.FilesSkipped >= 1);

            // Verify imported bars are from QQQ (first open = 200.00m), not SPY (297.26m)
            var firstBar = await ctx.FiveMinuteBars.OrderBy(b => b.Timestamp).FirstAsync();
            Assert.Equal(200.00m, firstBar.Open);
            Assert.False(await ctx.FiveMinuteBars.AnyAsync(b => b.Open == 297.26m));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
