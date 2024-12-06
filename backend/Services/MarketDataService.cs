using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

public class MarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly string _apiKey;
    private const string ApiUrlTemplate = "https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=SPY&interval=5min&month={0}&outputsize=full&extended_hours=false&apikey={1}";


    public MarketDataService(HttpClient httpClient, AppDbContext context, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new InvalidOperationException("API key not found.");
    }

    public async Task EnsureInitialMonthlyData()
    {
        if (await _context.MarketDataMonths.AnyAsync())
        {
            Console.WriteLine("Market data already exists. Skipping initial fetch.");
            return;
        }

        await FetchRandomHistoricalMonth();
    }

    public async Task FetchRandomHistoricalMonth()
    {
        var randomMonth = GetRandomMonthWithinLastTenYears();
        Console.WriteLine($"Fetching data for initial random month: {randomMonth:yyyy-MM}");
        await FetchAndSaveMonthlyData(randomMonth);
    }

    public async Task FetchNextUniqueMonth()
    {
        var uniqueMonth = await GetUniqueMonthFromAPI();
        if (uniqueMonth == null)
        {
            Console.WriteLine("No new unique months to fetch.");
            return;
        }

        Console.WriteLine($"Fetching data for unique month: {uniqueMonth.Value:yyyy-MM}");
        await FetchAndSaveMonthlyData(uniqueMonth.Value);
    }

    private DateTime GetRandomMonthWithinLastTenYears()
    {
        var random = new Random();
        var endDate = DateTime.Now;
        var startDate = endDate.AddYears(-10);

        var year = random.Next(startDate.Year, endDate.Year + 1);
        var month = random.Next(1, 13);

        if (year == endDate.Year && month > endDate.Month)
            month = endDate.Month;

        return new DateTime(year, month, 1);
    }

    private async Task<DateTime?> GetUniqueMonthFromAPI()
    {
        var random = new Random();
        var endDate = DateTime.Now;
        var startDate = endDate.AddYears(-10);

        for (int i = 0; i < 10; i++) // Attempt multiple times to find a unique month
        {
            var year = random.Next(startDate.Year, endDate.Year + 1);
            var month = random.Next(1, 13);

            if (year == endDate.Year && month > endDate.Month)
                continue;

            var candidateMonth = new DateTime(year, month, 1);

            if (!await _context.MarketDataMonths.AnyAsync(m => m.Month.Year == candidateMonth.Year && m.Month.Month == candidateMonth.Month))
                return candidateMonth;
        }

        return null;
    }

    public async Task FetchAndSaveMonthlyData(DateTime targetMonth)
    {
        var monthString = targetMonth.ToString("yyyy-MM");
        var apiUrl = string.Format(ApiUrlTemplate, monthString, _apiKey);

        string response;
        try
        {
            response = await _httpClient.GetStringAsync(apiUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data: {ex.Message}");
            return;
        }

        var responseObj = JsonConvert.DeserializeObject<MarketDataResponse>(response);

        if (responseObj?.TimeSeries == null)
        {
            Console.WriteLine($"No data found for the target month: {targetMonth:yyyy-MM}");
            return;
        }

        var month = new MarketDataMonth
        {
            Month = targetMonth
        };

        _context.MarketDataMonths.Add(month);
        await _context.SaveChangesAsync();

        var groupedByDay = responseObj.TimeSeries
            .OrderBy(entry => DateTime.Parse(entry.Key))
            .GroupBy(entry => DateTime.Parse(entry.Key).Date)
            .ToDictionary(
                g => g.Key,
                g => g.Select(entry => new FiveMinuteBar
                {
                    Timestamp = DateTime.Parse(entry.Key),
                    Open = entry.Value.Open,
                    High = entry.Value.High,
                    Low = entry.Value.Low,
                    Close = entry.Value.Close
                }).ToList()
            );

        foreach (var dayData in groupedByDay.OrderBy(d => d.Key))
        {
            var day = new MarketDataDay
            {
                Date = dayData.Key,
                MarketDataMonthId = month.Id,
                FiveMinuteBars = dayData.Value
            };

            foreach (var bar in day.FiveMinuteBars)
            {
                bar.MarketDataDayId = day.Id;
            }

            _context.MarketDataDays.Add(day);
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"Data for {targetMonth:yyyy-MM} saved successfully.");
    }


}
