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
        string? canonicalSymbol = null; // Fix 2: track first-seen symbol across the loop

        foreach (var path in Directory.GetFiles(_cacheDir, "*.json").OrderBy(p => p))
        {
            processed++;

            var fileName = Path.GetFileName(path);

            if (!TryParseMonthFromFileName(fileName, out var targetMonth))
            {
                Console.WriteLine($"Skipping unrecognized file name: {fileName}");
                skipped++;
                continue;
            }

            // Fix 2: extract symbol and enforce single-symbol constraint
            if (!TryParseSymbolFromFileName(fileName, out var symbol))
            {
                Console.WriteLine($"Skipping unrecognized file name: {fileName}");
                skipped++;
                continue;
            }

            if (canonicalSymbol is null)
            {
                canonicalSymbol = symbol;
            }
            else if (symbol != canonicalSymbol)
            {
                Console.WriteLine($"Skipping {fileName}: cache dir contains multiple symbols; only '{canonicalSymbol}' is imported.");
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
                Console.WriteLine($"Skipping unparseable file {fileName}: {ex.Message}");
                skipped++;
                continue;
            }

            if (parsed?.TimeSeries == null || parsed.TimeSeries.Count == 0)
            {
                Console.WriteLine($"Skipping empty file: {fileName}");
                skipped++;
                continue;
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            var month = new MarketDataMonth { Month = targetMonth };
            _db.MarketDataMonths.Add(month);
            await _db.SaveChangesAsync();   // populates month.Id; not yet durable

            var byDay = parsed.TimeSeries
                .OrderBy(e => DateTime.Parse(e.Key))
                .GroupBy(e => DateTime.Parse(e.Key).Date);

            int monthBars = 0;
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
                monthBars += day.FiveMinuteBars.Count;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            bars += monthBars;
            months++;
            _db.ChangeTracker.Clear(); // Fix 1: prevent entity accumulation across months (~200k entities over 120 months)
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

    private static bool TryParseSymbolFromFileName(string fileName, out string symbol)
    {
        // Expected: {SYMBOL}-{yyyy-MM}.json  e.g. SPY-2020-03.json
        // Returns the part before the first dash (the ticker symbol).
        symbol = string.Empty;
        var name = Path.GetFileNameWithoutExtension(fileName);
        var dash = name.IndexOf('-');
        if (dash < 0) return false;
        symbol = name[..dash];
        return symbol.Length > 0;
    }
}
