using System.Collections.Generic;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Editor-configurable zone progression rules and per-kind wheel selection.
    /// Safe zones (every <see cref="SafeZoneInterval"/>) use the safe wheel, Super zones
    /// (every <see cref="SuperZoneInterval"/>) use the super wheel, every other zone uses
    /// the normal wheel.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Zone Configuration", fileName = "Zones_")]
    public sealed class ZoneConfiguration : ScriptableObject
    {
        [SerializeField, Min(1)] private int startingZone = 1;
        [SerializeField, Min(1)] private int safeZoneInterval = 5;
        [SerializeField, Min(1)] private int superZoneInterval = 30;
        [Tooltip("Zone whose reward finishes the run (banks everything and shows the summary). 0 keeps the run endless.")]
        [SerializeField, Min(0)] private int finalZone = 60;

        [Header("Wheels")]
        [Tooltip("Used for normal zones (not safe or super).")]
        [SerializeField] private WheelDefinition normalWheel;
        [Tooltip("Used for safe zones (every safe zone interval).")]
        [SerializeField] private WheelDefinition safeWheel;
        [Tooltip("Used for super zones (every super zone interval).")]
        [SerializeField] private WheelDefinition superWheel;

        public int StartingZone => startingZone;
        public int SafeZoneInterval => safeZoneInterval;
        public int SuperZoneInterval => superZoneInterval;
        public int FinalZone => finalZone;

        public WheelDefinition NormalWheel => normalWheel;
        public WheelDefinition SafeWheel => safeWheel;
        public WheelDefinition SuperWheel => superWheel;

        public ZoneRules ToRules() => new ZoneRules(safeZoneInterval, superZoneInterval);

        /// <summary>Returns the wheel whose kind matches the classified zone.</summary>
        public WheelDefinition GetWheelForZone(int zone)
        {
            return GetWheel(ZoneClassifier.Classify(zone, ToRules()));
        }

        /// <summary>Returns the wheel configured for the given zone kind.</summary>
        public WheelDefinition GetWheel(ZoneKind kind)
        {
            switch (kind)
            {
                case ZoneKind.Super:
                    return superWheel != null ? superWheel : normalWheel;
                case ZoneKind.Safe:
                    return safeWheel != null ? safeWheel : normalWheel;
                default:
                    return normalWheel;
            }
        }

        /// <summary>All distinct wheels referenced by this configuration.</summary>
        public IEnumerable<WheelDefinition> AllWheels
        {
            get
            {
                if (normalWheel != null) yield return normalWheel;
                if (safeWheel != null) yield return safeWheel;
                if (superWheel != null) yield return superWheel;
            }
        }
    }
}
