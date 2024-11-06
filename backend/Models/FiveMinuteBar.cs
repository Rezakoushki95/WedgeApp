namespace backend.Models;

public class FiveMinuteBar
{
    public int Id { get; set; }
    public string? Date { get; set; }
    public string? Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}
