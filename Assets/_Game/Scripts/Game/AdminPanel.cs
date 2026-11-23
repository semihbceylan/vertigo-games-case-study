using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Hidden debug panel for the editor and development builds; it deactivates itself in release
    /// builds. Opened by tapping an invisible corner button several times in quick succession.
    /// Every command goes through the public session and wallet APIs, so the panel cannot put the
    /// game in a state normal play could not reach. It never owns flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AdminPanel : MonoBehaviour
    {
        #region Configuration

        private const float SHOWN_ALPHA = 1f;
        private const float HIDDEN_ALPHA = 0f;
        private const int NO_SLOT = -1;
        private const int FIRST_SLOT = 0;
        private const string SPEED_FORMAT = "Speed {0}x";

        [Header("Widgets")]
        [SerializeField] private CanvasGroup _window;
        [SerializeField] private UIButton _trigger;
        [SerializeField] private UIButton _close;
        [SerializeField] private TMP_Text _status;

        [Header("Zone")]
        [SerializeField] private UIButton _zoneBackLarge;
        [SerializeField] private UIButton _zoneBack;
        [SerializeField] private UIButton _zoneForward;
        [SerializeField] private UIButton _zoneForwardLarge;
        [SerializeField] private UIButton _nextSafe;
        [SerializeField] private UIButton _nextSuper;
        [SerializeField, Min(1)] private int _zoneStep = 1;
        [SerializeField, Min(1)] private int _largeZoneStep = 5;
        [SerializeField] private ZoneRuleDefinition _safeRule;
        [SerializeField] private ZoneRuleDefinition _superRule;

        [Header("Next spin")]
        [SerializeField] private UIButton _slotPrevious;
        [SerializeField] private UIButton _slotNext;
        [SerializeField] private UIButton _forceSlot;
        [SerializeField] private UIButton _forceBomb;

        [Header("Speed")]
        [SerializeField] private UIButton _speed;
        [SerializeField] private TMP_Text _speedLabel;
        [Tooltip("Animation speeds the speed button cycles through; applied to every DOTween tween.")]
        [SerializeField] private float[] _timeScales = { 1f, 0.5f, 0.25f };

        [Header("Currency")]
        [SerializeField] private UIButton _addGold;
        [SerializeField] private UIButton _addCash;
        [SerializeField] private UIButton _emptyGold;
        [SerializeField] private UIButton _resetBalances;
        [SerializeField] private CurrencyDefinition _gold;
        [SerializeField] private CurrencyDefinition _cash;
        [SerializeField, Min(1)] private long _grantAmount = 1000;

        [Header("Hidden trigger")]
        [SerializeField, Min(2)] private int _requiredTaps = 4;
        [Tooltip("All taps must land within this many seconds.")]
        [SerializeField, Min(0.1f)] private float _tapWindow = 1.5f;

        #endregion

        #region Runtime State

        private readonly StringBuilder _text = new StringBuilder();
        private GameSession _session;
        private ZoneCatalog _zones;
        private ForcedOutcomeRandom _spinRandom;
        private ICurrencyWallet _wallet;
        private IReadOnlyDictionary<string, long> _startingBalances;
        private ZoneWheel _forcedWheel;
        private int _selectedSlot;
        private int _speedIndex;
        private float _restoreTimeScale;
        private float _firstTapTime;
        private int _tapCount;
        private bool _isOpen;
        private bool _isSubscribed;
        private bool _isListening;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                // Release builds never expose the panel or its hidden trigger.
                gameObject.SetActive(false);
                return;
            }
            _restoreTimeScale = DOTween.timeScale;
            SetOpen(false);
        }

        private void OnEnable()
        {
            AddListeners();
            Subscribe();
        }

        private void OnDisable()
        {
            RemoveListeners();
            Unsubscribe();
            RestoreSpeed();
        }

        private void OnDestroy()
        {
            RestoreSpeed();
        }

        /// <summary>Supplied by the composition root in editor and development builds only.</summary>
        public void Bind(GameSession session, ZoneCatalog zones, ForcedOutcomeRandom spinRandom,
            ICurrencyWallet wallet, IReadOnlyDictionary<string, long> startingBalances)
        {
            Unsubscribe();
            _session = session;
            _zones = zones;
            _spinRandom = spinRandom;
            _wallet = wallet;
            _startingBalances = startingBalances;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            Refresh();
        }

        private void AddListeners()
        {
            if (_isListening)
            {
                return;
            }
            _trigger.Clicked += OnTriggerTapped;
            _close.Clicked += Close;
            _zoneBackLarge.Clicked += JumpBackLarge;
            _zoneBack.Clicked += JumpBack;
            _zoneForward.Clicked += JumpForward;
            _zoneForwardLarge.Clicked += JumpForwardLarge;
            _nextSafe.Clicked += JumpToNextSafe;
            _nextSuper.Clicked += JumpToNextSuper;
            _slotPrevious.Clicked += SelectPreviousSlot;
            _slotNext.Clicked += SelectNextSlot;
            _forceSlot.Clicked += ForceSelectedSlot;
            _forceBomb.Clicked += ForceBomb;
            _speed.Clicked += CycleSpeed;
            _addGold.Clicked += AddGold;
            _addCash.Clicked += AddCash;
            _emptyGold.Clicked += EmptyGold;
            _resetBalances.Clicked += ResetBalances;
            _isListening = true;
        }

        private void RemoveListeners()
        {
            if (!_isListening)
            {
                return;
            }
            _trigger.Clicked -= OnTriggerTapped;
            _close.Clicked -= Close;
            _zoneBackLarge.Clicked -= JumpBackLarge;
            _zoneBack.Clicked -= JumpBack;
            _zoneForward.Clicked -= JumpForward;
            _zoneForwardLarge.Clicked -= JumpForwardLarge;
            _nextSafe.Clicked -= JumpToNextSafe;
            _nextSuper.Clicked -= JumpToNextSuper;
            _slotPrevious.Clicked -= SelectPreviousSlot;
            _slotNext.Clicked -= SelectNextSlot;
            _forceSlot.Clicked -= ForceSelectedSlot;
            _forceBomb.Clicked -= ForceBomb;
            _speed.Clicked -= CycleSpeed;
            _addGold.Clicked -= AddGold;
            _addCash.Clicked -= AddCash;
            _emptyGold.Clicked -= EmptyGold;
            _resetBalances.Clicked -= ResetBalances;
            _isListening = false;
        }

        private void Subscribe()
        {
            if (_isSubscribed || _session == null)
            {
                return;
            }
            _session.Events.Subscribe<SessionChanged>(OnSessionChanged);
            _session.Events.Subscribe<CurrencyBalanceChanged>(OnBalanceChanged);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }
            _session.Events.Unsubscribe<SessionChanged>(OnSessionChanged);
            _session.Events.Unsubscribe<CurrencyBalanceChanged>(OnBalanceChanged);
            _isSubscribed = false;
        }

        #endregion

        #region Hidden Trigger

        private void OnTriggerTapped()
        {
            float now = Time.unscaledTime;
            if (_tapCount == 0 || now - _firstTapTime > _tapWindow)
            {
                _firstTapTime = now;
                _tapCount = 0;
            }
            _tapCount++;
            if (_tapCount < _requiredTaps)
            {
                return;
            }
            _tapCount = 0;
            SetOpen(!_isOpen);
        }

        private void Close()
        {
            SetOpen(false);
        }

        private void SetOpen(bool isOpen)
        {
            _isOpen = isOpen;
            _window.alpha = isOpen ? SHOWN_ALPHA : HIDDEN_ALPHA;
            _window.interactable = isOpen;
            _window.blocksRaycasts = isOpen;
            if (isOpen)
            {
                Refresh();
            }
        }

        #endregion

        #region Zone Commands

        private void JumpBackLarge()
        {
            JumpBy(-_largeZoneStep);
        }

        private void JumpBack()
        {
            JumpBy(-_zoneStep);
        }

        private void JumpForward()
        {
            JumpBy(_zoneStep);
        }

        private void JumpForwardLarge()
        {
            JumpBy(_largeZoneStep);
        }

        private void JumpToNextSafe()
        {
            JumpToNext(_safeRule);
        }

        private void JumpToNextSuper()
        {
            JumpToNext(_superRule);
        }

        private void JumpBy(int delta)
        {
            if (_session != null)
            {
                _session.JumpToZone(_session.Zone + delta);
            }
        }

        private void JumpToNext(ZoneRuleDefinition rule)
        {
            if (_session == null || !rule)
            {
                return;
            }
            int zone = _zones.FindNextZone(_session.Zone, rule);
            if (zone != ZoneRules.NO_ZONE)
            {
                _session.JumpToZone(zone);
            }
        }

        #endregion

        #region Next-Spin Commands

        private void SelectPreviousSlot()
        {
            StepSlot(-1);
        }

        private void SelectNextSlot()
        {
            StepSlot(1);
        }

        private void StepSlot(int delta)
        {
            if (_session == null)
            {
                return;
            }
            int count = _session.CurrentWheel.SliceCount;
            _selectedSlot = ((_selectedSlot + delta) % count + count) % count;
            Refresh();
        }

        private void ForceSelectedSlot()
        {
            Force(_selectedSlot);
        }

        private void ForceBomb()
        {
            int bomb = FindBombSlot();
            if (bomb != NO_SLOT)
            {
                Force(bomb);
            }
        }

        private void Force(int slot)
        {
            if (_spinRandom == null)
            {
                return;
            }
            _spinRandom.Force(slot);
            _forcedWheel = _session.CurrentWheel;
            Refresh();
        }

        private int FindBombSlot()
        {
            if (_session == null)
            {
                return NO_SLOT;
            }
            ZoneWheel wheel = _session.CurrentWheel;
            for (int i = FIRST_SLOT; i < wheel.SliceCount; i++)
            {
                if (wheel[i].IsBomb)
                {
                    return i;
                }
            }
            return NO_SLOT;
        }

        #endregion

        #region Speed

        private void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % _timeScales.Length;
            DOTween.timeScale = _timeScales[_speedIndex];
            Refresh();
        }

        private void RestoreSpeed()
        {
            if (_speedIndex == 0 && Mathf.Approximately(DOTween.timeScale, _restoreTimeScale))
            {
                return;
            }
            _speedIndex = 0;
            DOTween.timeScale = _restoreTimeScale;
        }

        #endregion

        #region Currency Commands

        private void AddGold()
        {
            Grant(_gold);
        }

        private void AddCash()
        {
            Grant(_cash);
        }

        private void Grant(CurrencyDefinition currency)
        {
            if (_wallet != null && currency)
            {
                _wallet.Credit(currency.CurrencyId, _grantAmount);
            }
        }

        private void EmptyGold()
        {
            SetBalance(_gold, 0);
        }

        private void ResetBalances()
        {
            ResetToStarting(_gold);
            ResetToStarting(_cash);
        }

        private void ResetToStarting(CurrencyDefinition currency)
        {
            long starting;
            if (currency && _startingBalances != null && _startingBalances.TryGetValue(currency.CurrencyId, out starting))
            {
                SetBalance(currency, starting);
            }
        }

        /// <summary>Moves a balance with real wallet transactions, so persistence and events stay consistent.</summary>
        private void SetBalance(CurrencyDefinition currency, long target)
        {
            if (_wallet == null || !currency)
            {
                return;
            }
            long current = _wallet.GetBalance(currency.CurrencyId);
            if (target > current)
            {
                _wallet.Credit(currency.CurrencyId, target - current);
            }
            else if (target < current)
            {
                _wallet.TrySpend(currency.CurrencyId, current - target);
            }
        }

        #endregion

        #region Readout

        private void OnSessionChanged(SessionChanged message)
        {
            // A forced slot belongs to the wheel it was chosen on; drop it if the wheel was replaced.
            if (_spinRandom != null && _spinRandom.HasForcedOutcome && _session.CurrentWheel != _forcedWheel)
            {
                _spinRandom.ClearForced();
            }
            if (_selectedSlot >= _session.CurrentWheel.SliceCount)
            {
                _selectedSlot = FIRST_SLOT;
            }
            Refresh();
        }

        private void OnBalanceChanged(CurrencyBalanceChanged message)
        {
            Refresh();
        }

        private void Refresh()
        {
            _speedLabel.text = string.Format(SPEED_FORMAT, _timeScales[_speedIndex]);
            if (_session == null || !_isOpen)
            {
                return;
            }
            bool isReady = _session.IsSpinAllowed;
            _zoneBackLarge.IsInteractable = isReady;
            _zoneBack.IsInteractable = isReady;
            _zoneForward.IsInteractable = isReady;
            _zoneForwardLarge.IsInteractable = isReady;
            _nextSafe.IsInteractable = isReady && _zones.FindNextZone(_session.Zone, _safeRule) != ZoneRules.NO_ZONE;
            _nextSuper.IsInteractable = isReady && _zones.FindNextZone(_session.Zone, _superRule) != ZoneRules.NO_ZONE;
            _forceSlot.IsInteractable = isReady;
            _forceBomb.IsInteractable = isReady && FindBombSlot() != NO_SLOT;

            ZoneWheel wheel = _session.CurrentWheel;
            WheelSlice selected = wheel[_selectedSlot];
            _text.Length = 0;
            _text.Append("State: ").Append(_session.StateName).AppendLine();
            _text.Append("Zone ").Append(_session.Zone).Append(" (").Append(_session.CurrentZoneKind.Id).Append(')').AppendLine();
            _text.Append("Revives left: ").Append(_session.RevivesRemaining)
                .Append("   Revive cost: ").Append(RewardAmountFormatter.Format(_session.ReviveCost)).AppendLine();
            _text.Append(_gold.DisplayName).Append(": ").Append(RewardAmountFormatter.Format(_wallet.GetBalance(_gold.CurrencyId)))
                .Append("   ").Append(_cash.DisplayName).Append(": ")
                .Append(RewardAmountFormatter.Format(_wallet.GetBalance(_cash.CurrencyId))).AppendLine();
            _text.Append("Slot ").Append(_selectedSlot + 1).Append('/').Append(wheel.SliceCount).Append(": ")
                .Append(selected.IsBomb ? "BOMB" : selected.Reward.RewardId + " x" + RewardAmountFormatter.Format(selected.Reward.Amount))
                .AppendLine();
            _text.Append("Next spin: ");
            if (_spinRandom != null && _spinRandom.HasForcedOutcome)
            {
                _text.Append("forced to slot ").Append(_spinRandom.ForcedOutcome + 1);
            }
            else
            {
                _text.Append("random");
            }
            _status.text = _text.ToString();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Validation

        private void OnValidate()
        {
            if (!_window || !_trigger || !_close || !_status || !_zoneBackLarge || !_zoneBack || !_zoneForward ||
                !_zoneForwardLarge || !_nextSafe || !_nextSuper || !_slotPrevious || !_slotNext || !_forceSlot ||
                !_forceBomb || !_speed || !_speedLabel || !_addGold || !_addCash || !_emptyGold || !_resetBalances ||
                !_gold || !_cash || !_safeRule || !_superRule)
            {
                Debug.LogError("AdminPanel has missing serialized references.", this);
            }
            if (_timeScales == null || _timeScales.Length == 0)
            {
                Debug.LogError("AdminPanel needs at least one animation speed.", this);
            }
        }

        #endregion
#endif
    }
}
