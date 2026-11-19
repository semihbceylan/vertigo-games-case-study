using System;
using System.Globalization;
using NUnit.Framework;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class RewardAmountFormatterTests
    {
        [TestCase(0L, "0")]
        [TestCase(999L, "999")]
        [TestCase(1000L, "1,000")]
        [TestCase(long.MaxValue, "9,223,372,036,854,775,807")]
        public void FormatsGroupedInteger(long amount, string expected)
        {
            Assert.AreEqual(expected, Format(amount));
        }

        [TestCase("tr-TR")]
        [TestCase("de-DE")]
        public void DeviceCultureDoesNotChangeRewardDisplay(string culture)
        {
            CultureInfo original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                Assert.AreEqual("1,234,567", Format(1234567));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        private static string Format(long amount)
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.RewardAmountFormatter, Assembly-CSharp", true);
            return (string)type.GetMethod("Format").Invoke(null, new object[] { amount });
        }
    }
}
