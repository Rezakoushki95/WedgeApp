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
