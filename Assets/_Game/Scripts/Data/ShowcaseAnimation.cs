using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Showcase", fileName = "Animation_Showcase")]
    public sealed class ShowcaseAnimation : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _popDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float _spinDuration = 14f;
        [SerializeField, Range(0f, 1f)] private float _hiddenAlpha;
        [SerializeField, Range(0f, 1f)] private float _visibleAlpha = 1f;
        [SerializeField, Min(0f)] private float _initialScale = 0.5f;
        [SerializeField, Min(0f)] private float _targetScale = 1f;
        [SerializeField] private Vector3 _spinRotation = new Vector3(0f, 0f, -360f);
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        [SerializeField] private Ease _popEase = Ease.OutBack;
        [SerializeField] private Ease _spinEase = Ease.Linear;

        #endregion

        #region Accessors

        public float FadeDuration { get { return _fadeDuration; } }
        public float PopDuration { get { return _popDuration; } }
        public float SpinDuration { get { return _spinDuration; } }
        public float HiddenAlpha { get { return _hiddenAlpha; } }
        public float VisibleAlpha { get { return _visibleAlpha; } }
        public float InitialScale { get { return _initialScale; } }
        public float TargetScale { get { return _targetScale; } }
        public Vector3 SpinRotation { get { return _spinRotation; } }
        public Ease FadeEase { get { return _fadeEase; } }
        public Ease PopEase { get { return _popEase; } }
        public Ease SpinEase { get { return _spinEase; } }

        #endregion
    }
}
