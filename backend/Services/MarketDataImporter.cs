using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace backend.Services;

public record ImportResult(int FilesProcessed, int FilesSkipped, int MonthsImported, int BarsImported);

public class MarketDataImporter
{
    private readonly AppDbContext _db;
    private readonly string _cacheDir;

    public MarketDataImporter(AppDbContext db, string cacheDir)
    {
        _db = db;
        _cacheDir = cacheDir;
    }

    public async Task<ImportResult> ImportAllAsync()
    {
        if (!Directory.Exists(_cacheDir))
            return new ImportResult(0, 0, 0, 0);

        int processed = 0, skipped = 0, months = 0, bars = 0;

        foreach (var path in Directory.GetFiles(_cacheDir, "*.json").OrderBy(p => p))
        {
            processed++;

            if (!TryParseMonthFromFileName(Path.GetFileName(path), out var targetMonth))
            {
                Console.WriteLine($"Skipping unrecognized file name: {Path.GetFileName(path)}");
                skipped++;
                continue;
            }

            if (await _db.MarketDataMonths
                    .AnyAsync(m => m.Month.Year == targetMonth.Year && m.Month.Month == targetMonth.Month))
            {
                skipped++;
                continue;
            }

            MarketDataResponse? parsed;
            try
            {
                parsed = JsonConvert.DeserializeObject<MarketDataResponse>(await File.ReadAllTextAsync(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping unparseable file {Path.GetFileName(path)}: {ex.Message}");
                skipped++;
                continue;
            }

            if (parsed?.TimeSeries == null || parsed.TimeSeries.Count == 0)
            {
                Console.WriteLine($"Skipping empty file: {Path.GetFileName(path)}");
                skipped++;
                continue;
            }

            var month = new MarketDataMonth { Month = targetMonth };
            _db.MarketDataMonths.Add(month);
            await _db.SaveChangesAsync();

            var byDay = parsed.TimeSeries
                .OrderBy(e => DateTime.Parse(e.Key))
                .GroupBy(e => DateTime.Parse(e.Key).Date);

            foreach (var dayGroup in byDay.OrderBy(g => g.Key))
            {
                var day = new MarketDataDay
                {
                    Date = dayGroup.Key,
                    MarketDataMonthId = month.Id,
                    FiveMinuteBars = dayGroup.Select(e => new FiveMinuteBar
                    {
                        Timestamp = DateTime.Parse(e.Key),
                        Open = e.Value.Open,
                        High = e.Value.High,
                        Low = e.Value.Low,
                        Close = e.Value.Close
                    }).ToList()
                };
                _db.MarketDataDays.Add(day);
                bars += day.FiveMinuteBars.Count;
            }

            await _db.SaveChangesAsync();
            months++;
        }

        return new ImportResult(processed, skipped, months, bars);
    }

    private static bool TryParseMonthFromFileName(string fileName, out DateTime month)
    {
        // Expected: {SYMBOL}-{yyyy-MM}.json  e.g. SPY-2020-03.json
        month = default;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var dash = name.IndexOf('-');
        if (dash < 0 || dash + 1 >= name.Length) return false;
        var datePart = name[(dash + 1)..]; // yyyy-MM
        return DateTime.TryParse(datePart + "-01", out month);
    }
}
