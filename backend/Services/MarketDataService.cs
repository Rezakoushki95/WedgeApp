using Newtonsoft.Json;
using backend.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using backend.Data;

namespace backend.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private const string ApiKey = "REMOVED_KEY";
        private const string ApiUrl = "https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=SPY&interval=5min&outputsize=full&extended_hours=false&apikey=" + ApiKey;

        // Dictionary to hold five-minute bars organized by date
        // private readonly Dictionary<string, List<FiveMinuteBar>> monthlyData = new();

        public MarketDataService(HttpClient httpClient, AppDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
        }

        // public async Task LoadMonthlyData()
        // {
        //     if (monthlyData.Count == 0) // Load only if data isn't already fetched
        //     {
        //         var response = await _httpClient.GetStringAsync(ApiUrl);
        //         ParseAndOrganizeData(response);
        //     }
        // }

        public async Task FetchAndSaveMonthlyData(int sessionId)
        {
            var response = await _httpClient.GetStringAsync(ApiUrl);
            var responseObj = JsonConvert.DeserializeObject<MarketDataResponse>(response);

            if (responseObj?.TimeSeries == null)
            {
                Console.WriteLine("Error: Invalid API response format.");
                return;
            }

            // Organize data by day, sorted by date and time within each day
            var groupedByDay = responseObj.TimeSeries
                .OrderBy(entry => DateTime.Parse(entry.Key)) // Sort by date and time
                .GroupBy(entry => entry.Key.Split(" ")[0]) // Group by date
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(entry => DateTime.Parse(entry.Key)) // Ensure bars are sorted within each day
                           .Select(entry =>
                               new FiveMinuteBar
                               {
                                   Date = g.Key,
                                   Time = entry.Key.Split(" ")[1],
                                   Open = entry.Value.Open,
                                   High = entry.Value.High,
                                   Low = entry.Value.Low,
                                   Close = entry.Value.Close
                               }).ToList()
                );

            // Save each day and its bars
            foreach (var dayData in groupedByDay)
            {
                var day = new Day
                {
                    TradingSessionId = sessionId,
                    Date = dayData.Key,
                    FiveMinuteBars = dayData.Value
                };

                _context.Days.Add(day);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Data saved successfully to the database.");
        }

    }

    //     private void ParseAndOrganizeData(string jsonResponse)
    //     {
    //         var responseObj = JsonConvert.DeserializeObject<MarketDataResponse>(jsonResponse);

    //         // Ensure the "Time Series (5min)" key exists and is not null
    //         if (responseObj?.TimeSeries == null)
    //         {
    //             Console.WriteLine("Error: Invalid API response format.");
    //             return;
    //         }

    //         // Sort and parse the data in chronological order
    //         var timeSeries = responseObj.TimeSeries.OrderBy(entry => DateTime.Parse(entry.Key));

    //         foreach (var entry in timeSeries)
    //         {
    //             var date = entry.Key.Split(" ")[0];
    //             var time = entry.Key.Split(" ")[1];

    //             var bar = new FiveMinuteBar
    //             {
    //                 Date = date,
    //                 Time = time,
    //                 Open = entry.Value.Open,
    //                 High = entry.Value.High,
    //                 Low = entry.Value.Low,
    //                 Close = entry.Value.Close
    //             };

    //             if (!monthlyData.ContainsKey(date))
    //             {
    //                 monthlyData[date] = new List<FiveMinuteBar>();
    //             }

    //             monthlyData[date].Add(bar);
    //         }
    //     }
    // }
}
