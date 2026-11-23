using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>A persistent currency. Adding a currency is a new asset, not a code change.</summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Currency", fileName = "Currency_")]
    public sealed class CurrencyDefinition : ScriptableObject
    {
        #region Serialized Values

        [SerializeField] private string _currencyId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;
        [Tooltip("Balance a new player starts with, before anything has been saved.")]
        [SerializeField, Min(0)] private long _startingBalance;

        #endregion

        #region Accessors

        public string CurrencyId
        {
            get { return _currencyId; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public Sprite Icon
        {
            get { return _icon; }
        }

        public long StartingBalance
        {
            get { return _startingBalance; }
        }

        #endregion
    }
}
