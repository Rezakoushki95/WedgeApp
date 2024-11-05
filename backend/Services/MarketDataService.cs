using Newtonsoft.Json;
using backend.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace backend.Services
{
    public class MarketDataService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "REMOVED_KEY";
        private const string ApiUrl = "https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol=SPY&interval=5min&outputsize=full&extended_hours=false&apikey=" + ApiKey;

        // Dictionary to hold five-minute bars organized by date
        private readonly Dictionary<string, List<FiveMinuteBar>> monthlyData = new();

        public MarketDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task LoadMonthlyData()
        {
            if (monthlyData.Count == 0) // Load only if data isn't already fetched
            {
                var response = await _httpClient.GetStringAsync(ApiUrl);
                ParseAndOrganizeData(response);
            }
        }

        private void ParseAndOrganizeData(string jsonResponse)
        {
            var responseObj = JsonConvert.DeserializeObject<MarketDataResponse>(jsonResponse);

            // Ensure the "Time Series (5min)" key exists and is not null
            if (responseObj?.TimeSeries == null)
            {
                Console.WriteLine("Error: Invalid API response format.");
                return;
            }

            // Sort and parse the data in chronological order
            var timeSeries = responseObj.TimeSeries.OrderBy(entry => DateTime.Parse(entry.Key));

            foreach (var entry in timeSeries)
            {
                var date = entry.Key.Split(" ")[0];
                var time = entry.Key.Split(" ")[1];

                var bar = new FiveMinuteBar
                {
                    Date = date,
                    Time = time,
                    Open = entry.Value.Open,
                    High = entry.Value.High,
                    Low = entry.Value.Low,
                    Close = entry.Value.Close
                };

                if (!monthlyData.ContainsKey(date))
                {
                    monthlyData[date] = new List<FiveMinuteBar>();
                }

                monthlyData[date].Add(bar);
            }
        }
    }




}
