using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>Serialized wheel-disc references survive typed cloning without runtime discovery.</summary>
    public sealed class WheelDiscView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private Image _base;
        [SerializeField] private WheelLayout _layout;

        public RectTransform Rect { get { return _rect; } }
        public Image Base { get { return _base; } }
        public WheelLayout Layout { get { return _layout; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_rect) _rect = (RectTransform)transform;
            if (!_layout) _layout = GetComponentInChildren<WheelLayout>(true);
            if (!_base)
            {
                foreach (Image image in GetComponentsInChildren<Image>(true))
                {
                    if (image.name == "ui_image_wheel_base_value") _base = image;
                }
            }
        }
#endif
    }
}
