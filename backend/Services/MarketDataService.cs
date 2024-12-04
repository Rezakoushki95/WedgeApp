using Newtonsoft.Json;
using backend.Models;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly string _apiKey;
        private const string ApiUrlTemplate = "https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=SPY&interval=5min&outputsize=full&extended_hours=false&apikey={0}";

        public MarketDataService(HttpClient httpClient, AppDbContext context, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new InvalidOperationException("API key not found.");
        }

        public async Task EnsureInitialMonthlyData()
        {
            var existingMonth = await _context.MarketDataMonths
                .OrderBy(m => m.Id)
                .FirstOrDefaultAsync();

            if (existingMonth != null)
            {
                Console.WriteLine("Initial data already exists. Skipping API fetch.");
                return;
            }

            await FetchAndSaveMonthlyData();
        }

        public async Task FetchAndSaveMonthlyData()
        {
            var apiUrl = string.Format(ApiUrlTemplate, _apiKey);

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
                Console.WriteLine("Error: Invalid API response format.");
                return;
            }

            var month = new MarketDataMonth
            {
                Month = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1) // First day of the current month
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
                        Timestamp = DateTime.Parse(entry.Key), // Set the full timestamp here
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
                    Date = dayData.Key, // The date of the day
                    MarketDataMonthId = month.Id,
                    FiveMinuteBars = dayData.Value
                };

                foreach (var bar in day.FiveMinuteBars.OrderBy(b => b.Timestamp))
                {
                    bar.MarketDataDayId = day.Id;
                }

                _context.MarketDataDays.Add(day);
            }


            await _context.SaveChangesAsync();
            Console.WriteLine("Data saved successfully in chronological order.");
        }
    }
}
