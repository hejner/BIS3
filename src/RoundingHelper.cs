namespace Taskr.Bis3;

public static class RoundingHelper
{
    public static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
