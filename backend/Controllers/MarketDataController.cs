using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MarketDataController : ControllerBase
    {

        private readonly MarketDataService _marketDataService;

        public MarketDataController(MarketDataService marketDataService)
        {
            _marketDataService = marketDataService;
        }

        [HttpGet("randomday")]
        public ActionResult<IEnumerable<FiveMinuteBar>> GetRandomDayData()
        {
            try
            {
                // Load the JSON data from the file
                var jsonData = System.IO.File.ReadAllText("Data/mockdata.json");

                // Deserialize the JSON data into a list of FiveMinuteBar objects
                var data = JsonConvert.DeserializeObject<List<FiveMinuteBar>>(jsonData);

                if (data != null && data.Count > 0)
                {
                    // Group the data by date
                    var groupedData = new Dictionary<string, List<FiveMinuteBar>>();
                    foreach (var entry in data)
                    {
                        if (entry.Date != null)
                        {
                            if (!groupedData.ContainsKey(entry.Date))
                            {
                                groupedData[entry.Date] = new List<FiveMinuteBar>();
                            }
                            groupedData[entry.Date].Add(entry);
                        }
                    }

                    // Randomly select a day's data to return
                    var random = new Random();
                    var randomDayData = groupedData.Values.ElementAt(random.Next(groupedData.Count));

                    return Ok(randomDayData);
                }
                else
                {
                    return NotFound("Data not found");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [HttpPost("fetch-data")]
        public async Task<IActionResult> FetchAndSaveData(int sessionId)
        {
            await _marketDataService.FetchAndSaveMonthlyData(sessionId);
            return Ok("Data fetched and saved successfully.");
        }


        // [HttpGet("test-parsing")]
        // public async Task<IActionResult> TestParsing()
        // {
        //     await _marketDataService.LoadMonthlyData();
        //     return Ok("Parsing test completed. Check console output for data sample.");
        // }
    }

}
