using System;
using System.Collections.Generic;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Session-scoped view of the zone configuration. Every zone is resolved by the core
    /// <see cref="ZoneRules"/> used by gameplay; the asset is only looked up for its visuals and wheel.
    /// </summary>
    public sealed class ZoneCatalog
    {
        #region Construction

        private readonly Dictionary<string, ZoneRuleDefinition> _definitions =
            new Dictionary<string, ZoneRuleDefinition>(StringComparer.Ordinal);

        public ZoneCatalog(ZoneConfiguration configuration)
        {
            if (!configuration)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            Configuration = configuration;
            Rules = configuration.ToRules();
            foreach (ZoneRuleDefinition definition in configuration.RuleDefinitions)
            {
                _definitions.Add(definition.Id, definition);
            }
        }

        #endregion

        #region Queries

        public ZoneConfiguration Configuration { get; }
        public ZoneRules Rules { get; }

        public int StartingZone
        {
            get { return Configuration.StartingZone; }
        }

        public int FinalZone
        {
            get { return Configuration.FinalZone; }
        }

        public ZoneRuleDefinition Resolve(int zone)
        {
            return _definitions[Rules.Resolve(zone).Id];
        }

        public WheelDefinition GetWheelForZone(int zone)
        {
            return Resolve(zone).Wheel;
        }

        public int FindNextZone(int currentZone, ZoneRuleDefinition rule)
        {
            return Rules.FindNextZone(currentZone, rule.Id, FinalZone);
        }

        #endregion
    }
}
