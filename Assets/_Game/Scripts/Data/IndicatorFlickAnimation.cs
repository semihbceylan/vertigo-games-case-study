using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>The pointer kicks aside when a slice boundary passes it, then springs back.</summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Indicator Flick", fileName = "Animation_IndicatorFlick")]
    public sealed class IndicatorFlickAnimation : ScriptableObject
    {
        #region Serialized Values

        [Tooltip("Pointer deflection in degrees. Negative swings the tip left, with the wheel's counter-clockwise spin.")]
        [SerializeField, Range(-45f, 45f)] private float _flickAngle = -14f;
        [SerializeField, Min(0.01f)] private float _flickDuration = 0.04f;
        [SerializeField] private Ease _flickEase = Ease.OutQuad;
        [SerializeField, Min(0.01f)] private float _returnDuration = 0.22f;
        [SerializeField] private Ease _returnEase = Ease.OutBack;

        #endregion

        #region Accessors

        public float FlickAngle { get { return _flickAngle; } }
        public float FlickDuration { get { return _flickDuration; } }
        public Ease FlickEase { get { return _flickEase; } }
        public float ReturnDuration { get { return _returnDuration; } }
        public Ease ReturnEase { get { return _returnEase; } }

        #endregion
    }
}
