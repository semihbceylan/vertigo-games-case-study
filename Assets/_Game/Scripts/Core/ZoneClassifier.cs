using System;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>
    /// Classifies a zone number into a <see cref="ZoneKind"/>.
    /// Super takes precedence over Safe (e.g. zone 30 is Super, not Safe).
    /// </summary>
    public static class ZoneClassifier
    {
        public static ZoneKind Classify(int zone, ZoneRules rules)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone), "Zone must be >= 1.");

            return rules.Resolve(zone);
        }

        /// <summary>Safe and Super zones never contain bombs.</summary>
        public static bool IsBombFree(ZoneKind kind)
        {
            return kind.IsBombFree;
        }
    }
}
