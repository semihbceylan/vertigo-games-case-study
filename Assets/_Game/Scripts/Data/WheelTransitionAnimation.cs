using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Wheel Transition", fileName = "Animation_WheelTransition")]
    public sealed class WheelTransitionAnimation : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(0f)] private float _holdDuration = 0.5f;
        [SerializeField, Min(0.01f)] private float _slideDuration = 0.35f;
        [SerializeField] private Ease _slideEase = Ease.InOutCubic;

        #endregion

        #region Accessors

        public float HoldDuration { get { return _holdDuration; } }
        public float SlideDuration { get { return _slideDuration; } }
        public Ease SlideEase { get { return _slideEase; } }

        #endregion
    }
}
