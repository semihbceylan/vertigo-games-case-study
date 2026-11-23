using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>Bomb hit: wheel shake, screen flash and a growing bomb icon before the loss panel opens.</summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Bomb Impact", fileName = "Animation_BombImpact")]
    public sealed class BombImpactAnimation : ScriptableObject
    {
        #region Serialized Values

        [Header("Shake")]
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.45f;
        [Tooltip("Maximum shake offset in canvas units.")]
        [SerializeField, Min(0f)] private float _shakeStrength = 28f;
        [SerializeField, Min(1)] private int _shakeVibrato = 22;
        [SerializeField, Range(0f, 180f)] private float _shakeRandomness = 90f;

        [Header("Flash")]
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Range(0f, 1f)] private float _flashPeakAlpha = 0.85f;
        [SerializeField, Range(0f, 1f)] private float _flashHiddenAlpha;
        [SerializeField, Min(0.01f)] private float _flashInDuration = 0.06f;
        [SerializeField] private Ease _flashInEase = Ease.OutQuad;
        [SerializeField, Min(0.01f)] private float _flashOutDuration = 0.35f;
        [SerializeField] private Ease _flashOutEase = Ease.InQuad;

        [Header("Bomb icon")]
        [Tooltip("Bomb icon scale relative to its resting scale.")]
        [SerializeField, Min(1f)] private float _iconScale = 1.6f;
        [SerializeField, Min(0.01f)] private float _iconDuration = 0.3f;
        [SerializeField] private Ease _iconEase = Ease.OutBack;

        [Header("Hand-off")]
        [Tooltip("Seconds after the hit before the loss panel starts to open; the shake may overlap it.")]
        [SerializeField, Min(0f)] private float _panelDelay = 0.55f;

        #endregion

        #region Accessors

        public float ShakeDuration { get { return _shakeDuration; } }
        public float ShakeStrength { get { return _shakeStrength; } }
        public int ShakeVibrato { get { return _shakeVibrato; } }
        public float ShakeRandomness { get { return _shakeRandomness; } }
        public Color FlashColor { get { return _flashColor; } }
        public float FlashPeakAlpha { get { return _flashPeakAlpha; } }
        public float FlashHiddenAlpha { get { return _flashHiddenAlpha; } }
        public float FlashInDuration { get { return _flashInDuration; } }
        public Ease FlashInEase { get { return _flashInEase; } }
        public float FlashOutDuration { get { return _flashOutDuration; } }
        public Ease FlashOutEase { get { return _flashOutEase; } }
        public float IconScale { get { return _iconScale; } }
        public float IconDuration { get { return _iconDuration; } }
        public Ease IconEase { get { return _iconEase; } }
        public float PanelDelay { get { return _panelDelay; } }

        #endregion
    }
}
