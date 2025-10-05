namespace Common.Helper
{
    public static class CalculateHelper
    {
        public static decimal CalculateAwardTotal(decimal casinoWinLoss)
        {
            if (casinoWinLoss >= 90000m)
            {
                return casinoWinLoss * 0.12m;
            }
            else if (casinoWinLoss >= 3000m)
            {
                return casinoWinLoss * 0.010m;
            }
            else if (casinoWinLoss <= 0)
            {
                return 0m;
            }
            else
            {
                return casinoWinLoss * 0.05m;
            }
        }
    }
}
