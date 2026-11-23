using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Authored map entries display the next zone resolved to their configured rule, with the
    /// rule's map icon beside it.
    /// </summary>
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
            [SerializeField] private Image _icon;

            public bool IsValid
            {
                get { return _rule && _root && _value && _icon; }
            }

            public void Refresh(int zone, ZoneCatalog zones)
            {
                _icon.sprite = _rule.MapIcon;
                _icon.enabled = _rule.MapIcon;
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

#if UNITY_EDITOR
        #region Editor Validation

        private void OnValidate()
        {
            foreach (MapEntry entry in _entries)
            {
                if (entry == null || !entry.IsValid)
                {
                    Debug.LogError("ZoneMapsView entries need a rule, root, value label and icon image.", this);
                }
            }
        }

        #endregion
#endif
    }
}
