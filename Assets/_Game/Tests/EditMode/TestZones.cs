using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    internal static class TestZones
    {
        public static readonly ZoneKind Normal = new ZoneKind("normal", false, false);
        public static readonly ZoneKind Safe = new ZoneKind("safe", true, true);
        public static readonly ZoneKind Super = new ZoneKind("super", true, true);

        public static ZoneRules CreateRules()
        {
            return new ZoneRules(new ZoneRule(Normal, 1, 1, 0, 0),
                new ZoneRule(Safe, 5, 1, 0, 1), new ZoneRule(Super, 30, 1, 0, 2));
        }
    }
}
