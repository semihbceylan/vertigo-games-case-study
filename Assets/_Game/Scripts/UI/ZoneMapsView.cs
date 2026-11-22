using System;
using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Authored map entries display the next zone resolved to their configured rule.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneMapsView : MonoBehaviour
    {
        #region Map Entries

        [Serializable]
        private sealed class MapEntry
        {
            [SerializeField] private ZoneRuleDefinition _rule;
            [SerializeField] private GameObject _root;
            [SerializeField] private TMP_Text _value;

            public void Refresh(int zone, ZoneCatalog zones)
            {
                int next = zones.FindNextZone(zone, _rule);
                bool hasNext = next != ZoneRules.NO_ZONE;
                _root.SetActive(hasNext);
                if (hasNext)
                {
                    _value.text = next.ToString();
                }
            }
        }

        [SerializeField] private MapEntry[] _entries = new MapEntry[0];

        #endregion

        #region Public API

        public void Set(int currentZone, ZoneCatalog zones)
        {
            foreach (MapEntry entry in _entries)
            {
                entry.Refresh(currentZone, zones);
            }
        }

        #endregion
    }
}
