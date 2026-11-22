using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>Zone strip slide and current-zone card cross-fade.</summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Zone Strip", fileName = "Animation_ZoneStrip")]
    public sealed class ZoneStripAnimation : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(0f)] private float _slideDuration = 0.35f;
        [SerializeField] private Ease _slideEase = Ease.InOutCubic;
        [Tooltip("Tint of the current-zone card and frame once fully shown.")]
        [SerializeField] private Color _visibleTint = Color.white;
        [Tooltip("Tint the card and frame fade from (incoming) or to (outgoing).")]
        [SerializeField] private Color _hiddenTint = new Color(1f, 1f, 1f, 0f);

        #endregion

        #region Accessors

        public float SlideDuration
        {
            get { return _slideDuration; }
        }

        public Ease SlideEase
        {
            get { return _slideEase; }
        }

        public Color VisibleTint
        {
            get { return _visibleTint; }
        }

        public Color HiddenTint
        {
            get { return _hiddenTint; }
        }

        #endregion
    }
}
