using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    public sealed class ResultTileView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _count;

        public Image Icon { get { return _icon; } }
        public TMP_Text Count { get { return _count; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_icon) _icon = GetComponentInChildren<Image>(true);
            if (!_count) _count = GetComponentInChildren<TMP_Text>(true);
        }
#endif
    }
}
