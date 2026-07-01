using System.Net;
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
        Assert.Equal(0, result.Skipped);
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

    [Fact]
    public async Task FetchRangeAsync_StopsAndDoesNotWriteOnNon200()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var fetcher = new MarketDataFetcher(
            new HttpClient(handler), _cacheDir, "KEY", dailyCap: 25, delay: _ => Task.CompletedTask);

        var result = await fetcher.FetchRangeAsync("SPY", 2020, 2020);

        Assert.Equal(0, result.Fetched);
        Assert.False(result.RangeComplete);
        Assert.Empty(Directory.GetFiles(_cacheDir, "*.json"));
    }

    [Fact]
    public async Task FetchRangeAsync_StopsOnNoteRateLimit()
    {
        var noteBody = "{\"Note\":\"Thank you for using Alpha Vantage! Our standard API rate limit is 25 requests per day.\"}";
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(noteBody));
        var fetcher = new MarketDataFetcher(
            new HttpClient(handler), _cacheDir, "KEY", dailyCap: 25, delay: _ => Task.CompletedTask);

        var result = await fetcher.FetchRangeAsync("SPY", 2020, 2020);

        Assert.Equal(0, result.Fetched);
        Assert.Empty(Directory.GetFiles(_cacheDir, "*.json"));
    }
}
