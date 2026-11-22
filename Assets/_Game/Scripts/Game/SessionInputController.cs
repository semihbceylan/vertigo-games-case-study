using System;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.UI;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Routes local button notifications to session commands with direct calls. Acceptance is
    /// decided by the session's current state object, so adding a state needs no change here.
    /// </summary>
    public sealed class SessionInputController
    {
        #region Construction

        private readonly GameSession _session;
        private readonly IClickSource _spinButton;
        private readonly IClickSource _leaveButton;
        private readonly ILossPanelInput _lossPanel;
        private readonly IResultsInput _results;
        private bool _isBound;

        public SessionInputController(GameSession session, IClickSource spinButton, IClickSource leaveButton,
            ILossPanelInput lossPanel, IResultsInput results)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _spinButton = spinButton;
            _leaveButton = leaveButton;
            _lossPanel = lossPanel;
            _results = results;
        }

        #endregion

        #region Binding

        /// <summary>Idempotent so repeated enable cycles never double-register listeners.</summary>
        public void Bind()
        {
            if (_isBound)
            {
                return;
            }
            _spinButton.Clicked += OnSpinClicked;
            _leaveButton.Clicked += OnLeaveClicked;
            _lossPanel.RestartRequested += OnRestartRequested;
            _lossPanel.ReviveRequested += OnReviveRequested;
            _results.RestartRequested += OnRestartRequested;
            _isBound = true;
        }

        public void Unbind()
        {
            if (!_isBound)
            {
                return;
            }
            _spinButton.Clicked -= OnSpinClicked;
            _leaveButton.Clicked -= OnLeaveClicked;
            _lossPanel.RestartRequested -= OnRestartRequested;
            _lossPanel.ReviveRequested -= OnReviveRequested;
            _results.RestartRequested -= OnRestartRequested;
            _isBound = false;
        }

        #endregion

        #region Commands

        private void OnSpinClicked()
        {
            if (_session.IsSpinAllowed)
            {
                _session.BeginSpin();
            }
        }

        private void OnLeaveClicked()
        {
            if (_session.IsLeaveAllowed)
            {
                _session.Leave();
            }
        }

        private void OnRestartRequested()
        {
            if (_session.IsRestartAllowed)
            {
                _session.Restart();
            }
        }

        private void OnReviveRequested()
        {
            if (_session.IsReviveAllowed && !_session.TryRevive())
            {
                _lossPanel.ShowInsufficientFunds();
            }
        }

        #endregion
    }
}
