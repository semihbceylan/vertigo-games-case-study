using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Reference holder for the wheel hierarchy. Resolves child references by name in the editor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelView : MonoBehaviour
    {
        [SerializeField] private RectTransform wheelRotation;
        [SerializeField] private Image wheelBase;
        [SerializeField] private Image wheelIndicator;
        [SerializeField] private RectTransform slicesRoot;
        [SerializeField] private Button spinButton;

        public RectTransform WheelRotation => wheelRotation;
        public Image WheelBase => wheelBase;
        public Image WheelIndicator => wheelIndicator;
        public RectTransform SlicesRoot => slicesRoot;
        public Button SpinButton => spinButton;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (wheelRotation == null)
            {
                var t = transform.Find("ui_wheel_rotation");
                if (t != null) wheelRotation = (RectTransform)t;
            }

            if (wheelRotation != null)
            {
                if (wheelBase == null)
                {
                    var t = wheelRotation.Find("ui_image_wheel_base");
                    if (t != null) wheelBase = t.GetComponent<Image>();
                }

                if (slicesRoot == null)
                {
                    var t = wheelRotation.Find("ui_wheel_slices");
                    if (t != null) slicesRoot = (RectTransform)t;
                }
            }

            if (wheelIndicator == null)
            {
                var t = transform.Find("ui_image_wheel_indicator");
                if (t != null) wheelIndicator = t.GetComponent<Image>();
            }

            if (spinButton == null)
            {
                var t = transform.Find("ui_button_spin");
                if (t != null) spinButton = t.GetComponent<Button>();
            }
        }
#endif
    }
}
