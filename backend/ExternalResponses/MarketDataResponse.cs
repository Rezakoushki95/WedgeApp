// Wrapper class for the API response
using Newtonsoft.Json;

public class MarketDataResponse
{
    [JsonProperty("Meta Data")]
    public Dictionary<string, string>? MetaData { get; set; }

    [JsonProperty("Time Series (5min)")]
    public Dictionary<string, BarData>? TimeSeries { get; set; }
}