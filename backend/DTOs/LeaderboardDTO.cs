namespace backend.DTOs;
public class LeaderboardDTO
{
    public string Username { get; set; } = string.Empty;
    public double TotalProfit { get; set; }
    public int TotalOrders { get; set; }
    public int TotalTradingDays { get; set; }
    public int Rank { get; set; }
    public double ProfitPerDay { get; set; }
}
