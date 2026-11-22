using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>The live wheel: its rotating disc plus the tier-specific indicator.</summary>
    public interface IWheelView
    {
        IWheelDisc Disc { get; }
        void SetIndicator(Sprite sprite);
    }

    /// <summary>Serialized references for the live wheel disc and its tier indicator.</summary>
    [DisallowMultipleComponent]
    public sealed class WheelView : MonoBehaviour, IWheelView
    {
        #region Serialized References

        [SerializeField] private WheelDiscView _disc;
        [SerializeField, FormerlySerializedAs("wheelIndicator")] private Image _indicator;

        #endregion

        #region Public API

        public IWheelDisc Disc
        {
            get { return _disc; }
        }

        public void SetIndicator(Sprite sprite)
        {
            _indicator.sprite = sprite;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_disc)
            {
                _disc = GetComponentInChildren<WheelDiscView>(true);
            }
            if (!_disc || !_indicator)
            {
                Debug.LogError("WheelView requires serialized disc and indicator references.", this);
            }
        }

        #endregion
#endif
    }
}
