using System;
using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Authored map entries display the next matching configured zone rule.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneMapsView : MonoBehaviour
    {
        [Serializable]
        private sealed class MapEntry
        {
            [SerializeField] private ZoneRuleDefinition _rule;
            [SerializeField] private GameObject _root;
            [SerializeField] private TMP_Text _value;

            public void Refresh(int zone, ZoneConfiguration configuration)
            {
                int next = configuration.FindNextZone(zone, _rule);
                _root.SetActive(next > 0);
                if (next > 0) _value.text = next.ToString();
            }
        }

        [SerializeField] private MapEntry[] _entries = new MapEntry[0];

        public void Set(int currentZone, ZoneConfiguration configuration)
        {
            foreach (MapEntry entry in _entries)
            {
                entry.Refresh(currentZone, configuration);
            }
        }
    }
}
