using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    public sealed class RewardRowView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _amount;
        [SerializeField] private CanvasGroup _group;

        public RectTransform Rect { get { return _rect; } }
        public Image Icon { get { return _icon; } }
        public TMP_Text Amount { get { return _amount; } }
        public CanvasGroup Group { get { return _group; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_rect) _rect = (RectTransform)transform;
            if (!_icon) _icon = GetComponentInChildren<Image>(true);
            if (!_amount) _amount = GetComponentInChildren<TMP_Text>(true);
            if (!_group) _group = GetComponent<CanvasGroup>();
        }
#endif
    }
}
