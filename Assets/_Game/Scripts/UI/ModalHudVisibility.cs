using UnityEngine;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Captures once per modal opening; repeated Show calls cannot overwrite the snapshot.</summary>
    public sealed class ModalHudVisibility
    {
        #region Snapshot

        private readonly CanvasGroup[] _groups;
        private readonly State[] _states;
        private bool _isHidden;

        private struct State
        {
            public float Alpha;
            public bool IsInteractable;
            public bool IsBlockingRaycasts;
        }

        public ModalHudVisibility(CanvasGroup[] groups)
        {
            _groups = groups;
            _states = new State[groups.Length];
        }

        public void Hide()
        {
            if (_isHidden) return;
            _isHidden = true;
            for (int i = 0; i < _groups.Length; i++)
            {
                CanvasGroup group = _groups[i];
                if (!group) continue;
                _states[i] = new State
                {
                    Alpha = group.alpha,
                    IsInteractable = group.interactable,
                    IsBlockingRaycasts = group.blocksRaycasts
                };
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        public void Restore()
        {
            if (!_isHidden) return;
            _isHidden = false;
            for (int i = 0; i < _groups.Length; i++)
            {
                CanvasGroup group = _groups[i];
                if (!group) continue;
                group.alpha = _states[i].Alpha;
                group.interactable = _states[i].IsInteractable;
                group.blocksRaycasts = _states[i].IsBlockingRaycasts;
            }
        }

        #endregion
    }
}
