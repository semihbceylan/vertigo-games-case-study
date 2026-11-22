using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Reward Fly", fileName = "Animation_RewardFly")]
    public sealed class RewardFlyAnimation : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(1f)] private float _iconSize = 120f;
        [SerializeField, Min(0.01f)] private float _popDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float _flyDuration = 0.55f;
        [SerializeField, Min(0f)] private float _popScale = 1.4f;
        [SerializeField, Min(0f)] private float _startScale = 0.6f;
        [SerializeField, Min(0f)] private float _endScale = 0.7f;
        [SerializeField] private Ease _popEase = Ease.OutBack;
        [SerializeField] private Ease _flyEase = Ease.InQuad;
        [SerializeField] private Ease _scaleEase = Ease.InQuad;

        #endregion

        #region Accessors

        public float IconSize { get { return _iconSize; } }
        public float PopDuration { get { return _popDuration; } }
        public float FlyDuration { get { return _flyDuration; } }
        public float PopScale { get { return _popScale; } }
        public float StartScale { get { return _startScale; } }
        public float EndScale { get { return _endScale; } }
        public Ease PopEase { get { return _popEase; } }
        public Ease FlyEase { get { return _flyEase; } }
        public Ease ScaleEase { get { return _scaleEase; } }

        #endregion
    }
}
