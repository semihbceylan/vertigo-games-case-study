using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Serialized widgets of one pooled results tile prefab.</summary>
    public sealed class ResultTileView : MonoBehaviour
    {
        #region Serialized References

        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _count;

        #endregion

        #region Public API

        public RectTransform Rect
        {
            get { return _rect; }
        }

        public Image Icon
        {
            get { return _icon; }
        }

        public TMP_Text Count
        {
            get { return _count; }
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_rect) _rect = (RectTransform)transform;
            if (!_icon) _icon = GetComponentInChildren<Image>(true);
            if (!_count) _count = GetComponentInChildren<TMP_Text>(true);
        }

        #endregion
#endif
    }
}
