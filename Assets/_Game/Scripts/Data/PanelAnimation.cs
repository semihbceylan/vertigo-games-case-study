using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Panel", fileName = "Animation_Panel")]
    public sealed class PanelAnimation : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _visibleAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float _hiddenAlpha;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;

        #endregion

        #region Accessors

        public float FadeDuration { get { return _fadeDuration; } }
        public float VisibleAlpha { get { return _visibleAlpha; } }
        public float HiddenAlpha { get { return _hiddenAlpha; } }
        public Ease FadeEase { get { return _fadeEase; } }

        #endregion
    }
}
