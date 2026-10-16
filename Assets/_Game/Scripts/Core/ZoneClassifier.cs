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

            if (zone % rules.SuperInterval == 0) return ZoneKind.Super;
            if (zone % rules.SafeInterval == 0) return ZoneKind.Safe;
            return ZoneKind.Normal;
        }

        /// <summary>Safe and Super zones never contain bombs.</summary>
        public static bool IsBombFree(ZoneKind kind) => kind == ZoneKind.Safe || kind == ZoneKind.Super;
    }
}
