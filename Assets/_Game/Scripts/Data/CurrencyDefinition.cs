using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Currency", fileName = "Currency_")]
    public sealed class CurrencyDefinition : ScriptableObject
    {
        [SerializeField] private string _currencyId;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

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
    }
}
