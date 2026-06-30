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
