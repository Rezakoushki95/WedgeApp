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
            // Check if there's already a month of data
            var existingMonth = await _context.MarketDataMonths
                .OrderBy(m => m.Id) // Ensures a consistent result for FirstOrDefault
                .Include(month => month.Days)
                .ThenInclude(day => day.FiveMinuteBars)
                .AsSplitQuery() // Improves performance by splitting the query
                .FirstOrDefaultAsync();


            if (existingMonth != null)
            {
                Console.WriteLine("Initial data already exists. Skipping API fetch.");
                return; // Data is already available, so no need to fetch
            }

            // Fetch a new month from the API and store it
            await FetchAndSaveMonthlyData();
        }

        public async Task<MarketDataDay?> GetUnaccessedDay(int userId)
        {
            // Retrieve unaccessed months for the user from the database
            var unaccessedMonths = await _context.MarketDataMonths
                .Where(m => !_context.AccessedMonths
                    .Any(a => a.UserId == userId && a.MarketDataMonthId == m.Id))
                .Include(m => m.Days)
                .ThenInclude(d => d.FiveMinuteBars)
                .ToListAsync();

            if (!unaccessedMonths.Any())
            {
                Console.WriteLine("No unaccessed months available for this user.");
                return null;
            }

            // Randomly select a month and then a day within that month
            var random = new Random();
            var randomMonth = unaccessedMonths[random.Next(unaccessedMonths.Count)];
            var unaccessedDaysInMonth = randomMonth.Days
                .Where(d => !_context.AccessedDays.Any(ad => ad.UserId == userId && ad.MarketDataDayId == d.Id))
                .ToList();

            if (!unaccessedDaysInMonth.Any())
            {
                Console.WriteLine("No unaccessed days in the selected month.");
                return null;
            }

            // Randomly pick a day from the unaccessed days
            var randomDay = unaccessedDaysInMonth[random.Next(unaccessedDaysInMonth.Count)];

            return randomDay;
        }



        public async Task FetchAndSaveMonthlyData()
        {
            var apiUrl = string.Format(ApiUrlTemplate, _apiKey);

            // Check if data already exists
            var existingMonth = await _context.MarketDataMonths
                .Include(month => month.Days)
                .ThenInclude(day => day.FiveMinuteBars)
                .FirstOrDefaultAsync();

            if (existingMonth != null)
            {
                Console.WriteLine("Existing market data found. No need to fetch.");
                return; // No need to fetch if we already have data
            }

            // Fetch data from API if no data exists
            var response = await _httpClient.GetStringAsync(apiUrl);
            var responseObj = JsonConvert.DeserializeObject<MarketDataResponse>(response);

            if (responseObj?.TimeSeries == null)
            {
                Console.WriteLine("Error: Invalid API response format.");
                return;
            }

            // Create a new month entry in the database
            var month = new MarketDataMonth
            {
                Month = DateTime.Now.ToString("yyyy-MM") // Set the current month
            };
            _context.MarketDataMonths.Add(month);
            await _context.SaveChangesAsync(); // Save to generate the Month ID

            // Organize data by day and sort by date and time
            var groupedByDay = responseObj.TimeSeries
                .OrderBy(entry => DateTime.Parse(entry.Key))
                .GroupBy(entry => entry.Key.Split(" ")[0]) // Group by date
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(entry => new FiveMinuteBar
                    {
                        Date = g.Key,
                        Time = entry.Key.Split(" ")[1],
                        Open = entry.Value.Open,
                        High = entry.Value.High,
                        Low = entry.Value.Low,
                        Close = entry.Value.Close
                    }).ToList()
                );

            // Save each day and its bars in chronological order
            foreach (var dayData in groupedByDay.OrderBy(d => DateTime.Parse(d.Key)))
            {
                var day = new MarketDataDay
                {
                    Date = dayData.Key,
                    MarketDataMonthId = month.Id,
                    FiveMinuteBars = dayData.Value
                };

                foreach (var bar in day.FiveMinuteBars.OrderBy(b => DateTime.Parse($"{b.Date} {b.Time}")))
                {
                    bar.MarketDataDayId = day.Id;
                }

                _context.MarketDataDays.Add(day);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Data saved successfully in chronological order.");
        }

        private void AssignExistingDataToSession(int userId)
        {
            // Retrieve all existing days in order
            var existingDays = _context.MarketDataDays
                .Include(day => day.FiveMinuteBars)
                .OrderBy(day => DateTime.Parse(day.Date))
                .ToList();

            foreach (var day in existingDays)
            {
                var accessedDay = new AccessedDay
                {
                    UserId = userId,
                    MarketDataDayId = day.Id
                };

                _context.AccessedDays.Add(accessedDay);
            }

            _context.SaveChanges();
        }
    }
}
