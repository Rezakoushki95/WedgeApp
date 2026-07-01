namespace backend.Models;

public enum TradeDirection
{
    Long = 0,
    Short = 1
}

public enum ExitReason
{
    Stop = 0,
    Manual = 1,
    EndOfDay = 2
}
