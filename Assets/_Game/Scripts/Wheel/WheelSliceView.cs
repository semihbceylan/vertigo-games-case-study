using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    public sealed class WheelSliceView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;

        public Image Icon { get { return _icon; } }
        public TMP_Text Amount { get { return _amount; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_icon) _icon = GetComponentInChildren<Image>(true);
            if (!_amount) _amount = GetComponentInChildren<TMP_Text>(true);
        }
#endif
    }
}
