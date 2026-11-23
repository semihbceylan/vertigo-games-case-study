using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Rolls a label from the amount it shows to a new amount. Interpolates progress rather than
    /// the value itself, so large amounts stay exact. Owned by the view that owns the label.
    /// </summary>
    public sealed class AmountCounter
    {
        #region State

        private const float START_PROGRESS = 0f;
        private const float END_PROGRESS = 1f;

        private readonly TMP_Text _label;
        private readonly GameObject _owner;
        private Tween _tween;
        private long _from;
        private long _target;
        private long _shown;
        private bool _hasValue;

        public AmountCounter(TMP_Text label, GameObject owner)
        {
            if (!label || !owner)
            {
                throw new ArgumentNullException(nameof(label), "A counter needs its label and owning object.");
            }
            _label = label;
            _owner = owner;
        }

        public bool IsCounting
        {
            get { return _tween != null; }
        }

        #endregion

        #region Commands

        /// <summary>Shows <paramref name="amount"/> immediately, cancelling any roll.</summary>
        public void Set(long amount)
        {
            Kill();
            _from = amount;
            _target = amount;
            _hasValue = true;
            Show(amount);
        }

        /// <summary>Rolls from the currently shown amount; the first value is shown without a roll.</summary>
        public void CountTo(long amount, CountUpAnimation animation)
        {
            if (!_hasValue)
            {
                Set(amount);
                return;
            }
            if (amount == _target)
            {
                return;
            }
            Kill();
            _from = _shown;
            _target = amount;
            _tween = DOVirtual.Float(START_PROGRESS, END_PROGRESS, animation.Duration, ApplyProgress)
                .SetEase(animation.Ease).SetUpdate(true).SetLink(_owner).OnComplete(FinishRoll);
        }

        /// <summary>Jumps an interrupted roll to its target so a re-enabled view never shows a stale number.</summary>
        public void Complete()
        {
            if (_tween == null)
            {
                return;
            }
            Kill();
            Show(_target);
        }

        public void Kill()
        {
            if (_tween != null)
            {
                _tween.Kill(false);
            }
            _tween = null;
        }

        #endregion

        #region Rolling

        private void ApplyProgress(float progress)
        {
            Show(_from + (long)Math.Round((_target - _from) * (double)progress));
        }

        private void FinishRoll()
        {
            _tween = null;
            Show(_target);
        }

        private void Show(long amount)
        {
            _shown = amount;
            if (_label)
            {
                _label.text = RewardAmountFormatter.Format(amount);
            }
        }

        #endregion
    }
}
