using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>Numbers roll from their old value to the new one instead of jumping.</summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Count Up", fileName = "Animation_CountUp")]
    public sealed class CountUpAnimation : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(0.01f)] private float _duration = 0.45f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        #endregion

        #region Accessors

        public float Duration { get { return _duration; } }
        public Ease Ease { get { return _ease; } }

        #endregion
    }
}
