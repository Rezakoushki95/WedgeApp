public class UpdateSessionDto
{
    public int SessionId { get; set; }
    public int? CurrentBarIndex { get; set; }
    public bool? HasOpenOrder { get; set; }
    public decimal? EntryPrice { get; set; }
    public decimal? TotalProfitLoss { get; set; }
    public int? TotalOrders { get; set; }
}
