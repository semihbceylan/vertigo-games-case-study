using System;
using System.Collections.Generic;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>
    /// Sole owner of gameplay and presentation flow. States accept commands polymorphically;
    /// views report operation completion rather than maintaining a second set of flow flags.
    /// A null presentation supports manually resolved spins in headless tests.
    /// </summary>
    public sealed class GameSession
    {
        #region Configuration and State

        private const int NO_PENDING_SLICE = -1;

        private readonly ZoneRules _rules;
        private readonly IZoneWheelFactory _factory;
        private readonly IRandomSource _random;
        private readonly IRandomSource _spinRandom;
        private readonly int _startingZone;
        private readonly int _finalZone;
        private readonly ISessionPresentation _presentation;
        private readonly ReadyState _ready;
        private readonly SpinningState _spinning;
        private readonly RewardPresentationState _rewardPresentation;
        private readonly ZoneTransitionState _zoneTransition;
        private readonly LostState _lost;
        private readonly CompletedState _completed;
        private SessionState _state;
        private int _operationVersion;
        private readonly RewardWallet _recoverableRewards = new RewardWallet();
        private readonly ICurrencyWallet _currencies;
        private readonly RevivePolicy _revivePolicy;
        private int _revivesUsed;

        private sealed class Completion
        {
            private readonly GameSession _session;
            private readonly int _version;
            private Action _callback;

            public Completion(GameSession session, Action callback)
            {
                _session = session;
                _version = session._operationVersion;
                _callback = callback;
            }

            public void Invoke()
            {
                Action callback = _callback;
                _callback = null;
                if (_session._operationVersion == _version)
                {
                    callback?.Invoke();
                }
            }
        }

        public GameSession(ZoneRules rules, IZoneWheelFactory factory, IRandomSource random,
            int startingZone = ZoneRules.FIRST_ZONE, int finalZone = ZoneRules.ENDLESS_FINAL_ZONE,
            IEventBus events = null, ISessionPresentation presentation = null,
            ICurrencyWallet currencies = null, RevivePolicy revivePolicy = null, IRandomSource spinRandom = null)
        {
            _rules = rules;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            // Spin outcomes may use their own source (a debug override) without touching wheel building.
            _spinRandom = spinRandom ?? random;
            _startingZone = Math.Max(ZoneRules.FIRST_ZONE, startingZone);
            _finalZone = Math.Max(ZoneRules.ENDLESS_FINAL_ZONE, finalZone);
            _presentation = presentation;
            _currencies = currencies;
            _revivePolicy = revivePolicy;
            Events = events ?? new EventBus();
            Wallet = new RewardWallet();
            _ready = new ReadyState(this);
            _spinning = new SpinningState(this);
            _rewardPresentation = new RewardPresentationState(this);
            _zoneTransition = new ZoneTransitionState(this);
            _lost = new LostState(this);
            _completed = new CompletedState(this);
            Zone = _startingZone;
            PendingSliceIndex = NO_PENDING_SLICE;
            _state = _ready;
            BuildWheel();
        }

        public RewardWallet Wallet { get; }
        public IReadOnlyDictionary<string, long> RecoverableRewards { get { return _recoverableRewards.Totals; } }
        public IEventBus Events { get; }
        public int Zone { get; private set; }
        public ZoneWheel CurrentWheel { get; private set; }
        public int PendingSliceIndex { get; private set; }
        public long BankedTotal { get; private set; }
        public ZoneKind CurrentZoneKind { get { return _rules.Resolve(Zone); } }
        public bool IsSpinAllowed { get { return _state.IsSpinAllowed; } }
        public bool IsLeaveAllowed { get { return _state.IsLeaveAllowed; } }
        public bool IsRestartAllowed { get { return _state.IsRestartAllowed; } }
        public bool IsReviveAllowed { get { return _state.IsReviveAllowed; } }
        public int RevivesRemaining { get { return _revivePolicy == null ? 0 : _revivePolicy.MaxRevives - _revivesUsed; } }
        public long ReviveCost { get { return _revivePolicy == null ? 0 : _revivePolicy.GetCost(Zone); } }
        public bool IsCompleted { get { return _state.IsCompleted; } }
        public bool IsZoneTransitioning { get { return _state.IsZoneTransitioning; } }

        /// <summary>Current state's type name, for diagnostics only; flow never branches on it.</summary>
        public string StateName { get { return _state.GetType().Name; } }

        #endregion

        #region Commands

        public void StartNewRun()
        {
            _operationVersion++;
            _presentation?.Cancel();
            Zone = _startingZone;
            BankedTotal = 0;
            _revivesUsed = 0;
            PendingSliceIndex = NO_PENDING_SLICE;
            Wallet.Clear();
            _recoverableRewards.Clear();
            BuildWheel();
            _presentation?.Reset();
            ChangeState(_ready);
        }

        public void BeginSpin()
        {
            _state.BeginSpin();
        }

        public void ResolvePendingSpin()
        {
            _state.ResolveSpin(PendingSliceIndex);
        }

        public void ResolveSpin(int sliceIndex)
        {
            _state.ResolveSpin(sliceIndex);
        }

        public void Leave()
        {
            _state.Leave();
        }

        public void Restart()
        {
            _state.Restart();
        }

        public bool TryRevive()
        {
            return _state.TryRevive();
        }

        /// <summary>Development command accepted only while ready, not during asynchronous work.</summary>
        public void JumpToZone(int zone)
        {
            _state.JumpToZone(zone);
        }

        #endregion

        #region Transitions

        private void ChangeState(SessionState state)
        {
            _operationVersion++;
            _state = state;
            Events.Publish(new SessionChanged());
            state.Enter();
        }

        private void BuildWheel()
        {
            CurrentWheel = _factory.CreateWheel(Zone, CurrentZoneKind, _random);
        }

        private void AdvanceAfterReward()
        {
            PendingSliceIndex = NO_PENDING_SLICE;
            if (_finalZone > ZoneRules.ENDLESS_FINAL_ZONE && Zone >= _finalZone)
            {
                ChangeState(_completed);
                return;
            }
            Zone++;
            BuildWheel();
            ChangeState(_zoneTransition);
        }

        private void FinishZoneTransition()
        {
            ChangeState(_ready);
        }

        #endregion

        #region State Implementations

        private abstract class SessionState
        {
            protected readonly GameSession Session;

            protected SessionState(GameSession session)
            {
                Session = session;
            }

            public virtual bool IsSpinAllowed { get { return false; } }
            public virtual bool IsLeaveAllowed { get { return false; } }
            public virtual bool IsRestartAllowed { get { return false; } }
            public virtual bool IsReviveAllowed { get { return false; } }

            public virtual bool TryRevive()
            {
                return false;
            }

            public virtual bool IsCompleted { get { return false; } }
            public virtual bool IsZoneTransitioning { get { return false; } }
            public virtual void Enter()
            {
            }

            public virtual void BeginSpin()
            {
                throw new InvalidOperationException("Spin is not allowed in the current state.");
            }

            public virtual void ResolveSpin(int sliceIndex)
            {
                throw new InvalidOperationException("No unresolved spin is in progress.");
            }

            public virtual void Leave()
            {
                throw new InvalidOperationException("Leaving is not allowed in the current state.");
            }

            public virtual void Restart()
            {
                throw new InvalidOperationException("Restart is only allowed after the run has ended.");
            }

            public virtual void JumpToZone(int zone)
            {
            }
        }

        private sealed class ReadyState : SessionState
        {
            public ReadyState(GameSession session) : base(session) { }
            public override bool IsSpinAllowed { get { return true; } }
            public override bool IsLeaveAllowed { get { return Session.CurrentZoneKind.IsLeaveAllowed; } }

            public override void BeginSpin()
            {
                Session.PendingSliceIndex = Session._spinRandom.NextInt(0, Session.CurrentWheel.SliceCount);
                Session.ChangeState(Session._spinning);
            }

            public override void Leave()
            {
                if (!IsLeaveAllowed)
                {
                    base.Leave();
                    return;
                }
                Session.ChangeState(Session._completed);
            }

            public override void JumpToZone(int zone)
            {
                zone = Math.Max(ZoneRules.FIRST_ZONE, zone);
                if (Session._finalZone > ZoneRules.ENDLESS_FINAL_ZONE)
                {
                    zone = Math.Min(zone, Session._finalZone);
                }
                Session.Zone = zone;
                Session.BuildWheel();
                Session._presentation?.Reset();
                Session.ChangeState(Session._ready);
            }
        }

        private sealed class SpinningState : SessionState
        {
            public SpinningState(GameSession session) : base(session) { }

            public override void Enter()
            {
                Session._presentation?.PlaySpin(Session.PendingSliceIndex, Session.CurrentWheel,
                    new Completion(Session, Session.ResolvePendingSpin).Invoke);
            }

            public override void ResolveSpin(int sliceIndex)
            {
                if (sliceIndex < 0 || sliceIndex >= Session.CurrentWheel.SliceCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(sliceIndex));
                }
                Session.PendingSliceIndex = sliceIndex;
                if (Session.CurrentWheel[sliceIndex].IsBomb)
                {
                    Session._recoverableRewards.Clear();
                    foreach (RewardGrant grant in Session.Wallet.Grants)
                    {
                        Session._recoverableRewards.Add(grant);
                    }
                    Session.Wallet.Clear();
                    // The bomb slot stays pending while lost so the loss presentation can show the hit.
                    Session.ChangeState(Session._lost);
                    return;
                }
                Session.ChangeState(Session._rewardPresentation);
            }
        }

        private sealed class RewardPresentationState : SessionState
        {
            public RewardPresentationState(GameSession session) : base(session) { }

            public override void Enter()
            {
                RewardGrant grant = Session.CurrentWheel[Session.PendingSliceIndex].Reward;
                Session.Wallet.Add(grant);
                if (Session._presentation == null)
                {
                    Session.AdvanceAfterReward();
                    return;
                }
                Session._presentation.PresentReward(grant, Session.PendingSliceIndex,
                    new Completion(Session, Session.AdvanceAfterReward).Invoke);
            }
        }

        private sealed class ZoneTransitionState : SessionState
        {
            public ZoneTransitionState(GameSession session) : base(session) { }
            public override bool IsZoneTransitioning { get { return true; } }

            public override void Enter()
            {
                if (Session._presentation == null)
                {
                    Session.FinishZoneTransition();
                    return;
                }
                Session._presentation.PlayZoneTransition(new Completion(Session, Session.FinishZoneTransition).Invoke);
            }
        }

        private sealed class LostState : SessionState
        {
            public LostState(GameSession session) : base(session) { }
            public override bool IsRestartAllowed { get { return true; } }
            public override bool IsReviveAllowed { get { return Session._currencies != null && Session._revivePolicy != null && Session.RevivesRemaining > 0; } }

            public override bool TryRevive()
            {
                if (!IsReviveAllowed)
                {
                    return false;
                }
                // Prepare content before charging so invalid configuration cannot consume currency.
                ZoneWheel wheel = Session._factory.CreateWheel(Session.Zone, Session.CurrentZoneKind, Session._random);
                if (!Session._currencies.TrySpend(Session._revivePolicy.CurrencyId, Session.ReviveCost))
                {
                    return false;
                }
                Session._revivesUsed++;
                Session.CurrentWheel = wheel;
                foreach (RewardGrant grant in Session._recoverableRewards.Grants)
                {
                    Session.Wallet.Add(grant);
                }
                Session._recoverableRewards.Clear();
                Session.PendingSliceIndex = NO_PENDING_SLICE;
                Session._presentation?.Reset();
                Session.ChangeState(Session._ready);
                return true;
            }

            public override void Enter()
            {
                Session._presentation?.ShowLoss(Session.Zone, Session.PendingSliceIndex);
            }

            public override void Restart()
            {
                Session.StartNewRun();
            }
        }

        private sealed class CompletedState : SessionState
        {
            public CompletedState(GameSession session) : base(session) { }
            public override bool IsCompleted { get { return true; } }
            public override bool IsRestartAllowed { get { return true; } }

            public override void Restart()
            {
                Session.StartNewRun();
            }

            public override void Enter()
            {
                Session.BankedTotal = Session.Wallet.AccumulatedAmount;
                Session.Events.Publish(new RunCompleted(Session.BankedTotal));
                Session._presentation?.ShowResults();
            }
        }

        #endregion
    }
}
