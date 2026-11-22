using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>Serialized wheel-disc references survive typed cloning without runtime discovery.</summary>
    public sealed class WheelDiscView : MonoBehaviour, IWheelDisc
    {
        #region Serialized References

        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _base;
        [SerializeField] private WheelLayout _layout;

        #endregion

        #region Public API

        public RectTransform Rect
        {
            get { return _rect; }
        }

        public Image Base
        {
            get { return _base; }
        }

        public WheelLayout Layout
        {
            get { return _layout; }
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_rect)
            {
                _rect = (RectTransform)transform;
            }
            if (!_layout)
            {
                _layout = GetComponentInChildren<WheelLayout>(true);
            }
            if (!_base || !_layout)
            {
                Debug.LogError("WheelDiscView requires serialized base and layout references.", this);
            }
        }

        #endregion
#endif
    }
}
