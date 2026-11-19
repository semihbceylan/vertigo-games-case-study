using System.Globalization;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Consistent grouped integer formatting for reward quantities, independent of device locale.</summary>
    public static class RewardAmountFormatter
    {
        private const string GROUPED_INTEGER_FORMAT = "N0";

        public static string Format(long amount)
        {
            return amount.ToString(GROUPED_INTEGER_FORMAT, CultureInfo.InvariantCulture);
        }
    }
}
