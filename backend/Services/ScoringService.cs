using backend.DTOs;
using backend.Models;

namespace backend.Services;

// Pure scoring math: R-multiples, compounding bankroll, and confidence-bounded
// Edge. No DB access here so it is trivially unit-testable.
public class ScoringService
{
    public const decimal StartingBankroll = 10_000m;
    private const decimal Z = 1.64m; // one-sided ~95% lower confidence bound

    // Maturity gates before a journey/pattern earns a confident Edge.
    private const int MinTrades = 30;
    private const int MinDistinctCharts = 10;

    // R = directional P&L expressed in units of the ORIGINAL stop distance.
    // Throws if the risk distance is zero (degenerate stop at entry price).
    public decimal ComputeR(TradeDirection direction, decimal entry, decimal originalStop, decimal exit)
    {
        var risk = Math.Abs(entry - originalStop);
        if (risk == 0m)
        {
            throw new ArgumentException("Original stop cannot equal the entry price (zero risk).");
        }

        var pnl = direction == TradeDirection.Long ? exit - entry : entry - exit;
        return pnl / risk;
    }

    // Bankroll compounds trade-by-trade: each trade moves it by R × riskFraction.
    public decimal ComputeBankroll(IEnumerable<Trade> tradesInOrder)
    {
        var bankroll = StartingBankroll;
        foreach (var t in tradesInOrder)
        {
            bankroll *= 1m + t.ResultR * t.RiskFraction;
        }
        return bankroll;
    }

    // Aggregate a trade ledger into the full stats card. `trades` is the pool
    // being measured (a journey, or all of a user's trades for a pattern).
    public JourneyStatsDTO ComputeStats(IReadOnlyList<Trade> trades)
    {
        var stats = new JourneyStatsDTO
        {
            TradeCount = trades.Count,
            Bankroll = StartingBankroll,
            EdgeState = EdgeState.Calibrating,
        };

        if (trades.Count == 0)
        {
            return stats;
        }

        var ordered = trades.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).ToList();
        var rs = ordered.Select(t => t.ResultR).ToList();

        stats.Bankroll = ComputeBankroll(ordered);
        stats.Expectancy = rs.Average();
        stats.DistinctCharts = ordered.Select(t => t.ChartId).Distinct().Count();

        var wins = rs.Where(r => r > 0m).ToList();
        var losses = rs.Where(r => r < 0m).ToList();
        stats.WinRate = (decimal)wins.Count / rs.Count;
        stats.AvgWinR = wins.Count > 0 ? wins.Average() : 0m;
        stats.AvgLossR = losses.Count > 0 ? losses.Average() : 0m;

        var grossWin = wins.Sum();
        var grossLoss = Math.Abs(losses.Sum());
        stats.ProfitFactor = grossLoss > 0m ? grossWin / grossLoss : (grossWin > 0m ? 999m : 0m);
        stats.MaxDrawdownR = ComputeMaxDrawdownR(rs);

        // Edge is computed on per-chart-day aggregated R so a burst of correlated
        // trades on one chart can't fake a tight confidence interval.
        var perChart = ordered
            .GroupBy(t => t.ChartId)
            .Select(g => g.Sum(t => t.ResultR))
            .ToList();

        var (edge, mean) = LowerBoundEdge(perChart);
        stats.Edge = edge;
        stats.EdgeState = ClassifyEdge(stats.TradeCount, stats.DistinctCharts, edge, mean);

        return stats;
    }

    // Edge = mean − Z·stderr over the per-chart-day R samples.
    private (decimal edge, decimal mean) LowerBoundEdge(IReadOnlyList<decimal> samples)
    {
        var n = samples.Count;
        if (n == 0) return (0m, 0m);

        var mean = samples.Average();
        if (n < 2) return (0m, mean); // no dispersion estimate yet → no credit

        var variance = samples.Sum(x => (x - mean) * (x - mean)) / (n - 1);
        var stdev = (decimal)Math.Sqrt((double)variance);
        var stderr = stdev / (decimal)Math.Sqrt(n);
        return (mean - Z * stderr, mean);
    }

    private EdgeState ClassifyEdge(int trades, int distinctCharts, decimal edge, decimal mean)
    {
        if (trades < MinTrades || distinctCharts < MinDistinctCharts)
        {
            return EdgeState.Calibrating;
        }
        if (edge <= 0m)
        {
            return EdgeState.Noise; // matured but the lower bound doesn't clear zero
        }
        // Clears zero with confidence; split on strength.
        return edge >= 0.1m ? EdgeState.Established : EdgeState.Promising;
    }

    private decimal ComputeMaxDrawdownR(IReadOnlyList<decimal> rs)
    {
        decimal cum = 0m, peak = 0m, maxDd = 0m;
        foreach (var r in rs)
        {
            cum += r;
            if (cum > peak) peak = cum;
            var dd = peak - cum;
            if (dd > maxDd) maxDd = dd;
        }
        return maxDd;
    }
}
