using System;

namespace Vertigo.WheelOfFortune.Core
{
    public enum SessionPhase
    {
        Idle,
        Spinning,
        Bust,
        Completed
    }

    /// <summary>
    /// Framework-independent gameplay state machine. Owns zone progression, spin eligibility,
    /// reward accumulation and bomb handling. Rendering and animation are handled elsewhere.
    /// </summary>
    public sealed class GameSession
    {
        private readonly ZoneRules _rules;
        private readonly IZoneWheelFactory _factory;
        private readonly IRandomSource _random;
        private readonly int _startingZone;
        private readonly int _finalZone;

        public GameSession(ZoneRules rules, IZoneWheelFactory factory, IRandomSource random, int startingZone = 1, int finalZone = 0)
        {
            _rules = rules;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _startingZone = startingZone < 1 ? 1 : startingZone;
            _finalZone = finalZone < 0 ? 0 : finalZone;

            Wallet = new RewardWallet();
            Zone = _startingZone;
            Phase = SessionPhase.Idle;
            BuildWheel();
        }

        public RewardWallet Wallet { get; }
        public int Zone { get; private set; }
        public SessionPhase Phase { get; private set; }
        public ZoneWheel CurrentWheel { get; private set; }

        /// <summary>Slice chosen before the animation starts, or -1 when not spinning.</summary>
        public int PendingSliceIndex { get; private set; } = -1;

        /// <summary>Total banked when the player leaves successfully.</summary>
        public long BankedTotal { get; private set; }

        public event Action StateChanged;
        public event Action<RewardGrant> RewardCollected;
        public event Action<int> BombHit;
        public event Action<int> ZoneAdvanced;
        public event Action<long> RunCompleted;
        public event Action RunRestarted;

        public ZoneKind CurrentZoneKind => ZoneClassifier.Classify(Zone, _rules);
        public bool IsIdle => Phase == SessionPhase.Idle;
        public bool CanSpin => Phase == SessionPhase.Idle;

        /// <summary>
        /// Leaving (banking the rewards) is possible while idle whenever the player has something
        /// to bank, or in a risk-free Safe/Super zone.
        /// </summary>
        public bool CanLeave => Phase == SessionPhase.Idle && (!Wallet.IsEmpty || ZoneClassifier.IsBombFree(CurrentZoneKind));
        public bool CanRestart => Phase == SessionPhase.Bust;
        public bool CanRevive => Phase == SessionPhase.Bust;

        private void BuildWheel()
        {
            CurrentWheel = _factory.CreateWheel(Zone, CurrentZoneKind, _random);
        }

        public void StartNewRun()
        {
            Zone = _startingZone;
            BankedTotal = 0;
            PendingSliceIndex = -1;
            Wallet.Clear();
            Phase = SessionPhase.Idle;
            BuildWheel();
            StateChanged?.Invoke();
        }

        /// <summary>Selects the outcome, then moves to the spinning phase. The animation completes it.</summary>
        public void BeginSpin()
        {
            if (!CanSpin)
                throw new InvalidOperationException("Spin is not allowed in phase " + Phase + ".");

            PendingSliceIndex = _random.NextInt(0, CurrentWheel.SliceCount);
            Phase = SessionPhase.Spinning;
            StateChanged?.Invoke();
        }

        /// <summary>Resolves the slice previously chosen by <see cref="BeginSpin"/>.</summary>
        public void ResolvePendingSpin()
        {
            if (Phase != SessionPhase.Spinning || PendingSliceIndex < 0)
                throw new InvalidOperationException("No spin is in progress.");

            ResolveSpin(PendingSliceIndex);
        }

        public void ResolveSpin(int sliceIndex)
        {
            if (Phase != SessionPhase.Spinning)
                throw new InvalidOperationException("No spin is in progress.");
            if (sliceIndex < 0 || sliceIndex >= CurrentWheel.SliceCount)
                throw new ArgumentOutOfRangeException(nameof(sliceIndex));

            var slice = CurrentWheel[sliceIndex];
            PendingSliceIndex = -1;

            if (slice.IsBomb)
            {
                // Rewards are kept on a bust so a revive can continue the same run.
                // They are only wiped when the player restarts (see StartNewRun).
                int bustedZone = Zone;
                Phase = SessionPhase.Bust;
                BombHit?.Invoke(bustedZone);
            }
            else
            {
                Wallet.Add(slice.Reward);
                RewardCollected?.Invoke(slice.Reward);

                // The final zone banks the run once its reward is collected (e.g. the super zone).
                if (_finalZone > 0 && Zone >= _finalZone)
                {
                    BankedTotal = Wallet.AccumulatedAmount;
                    Phase = SessionPhase.Completed;
                    StateChanged?.Invoke();
                    RunCompleted?.Invoke(BankedTotal);
                    return;
                }

                Zone++;
                Phase = SessionPhase.Idle;
                BuildWheel();
                ZoneAdvanced?.Invoke(Zone);
            }

            StateChanged?.Invoke();
        }

        /// <summary>Banks the accumulated rewards. Possible while idle once rewards exist or in a Safe/Super zone.</summary>
        public void Leave()
        {
            if (!CanLeave)
                throw new InvalidOperationException("Cannot leave in phase " + Phase + " while in a " + CurrentZoneKind + " zone.");

            BankedTotal = Wallet.AccumulatedAmount;
            Phase = SessionPhase.Completed;
            StateChanged?.Invoke();
            RunCompleted?.Invoke(BankedTotal);
        }

        /// <summary>Restarts the run after a bomb.</summary>
        public void Restart()
        {
            if (!CanRestart)
                throw new InvalidOperationException("Restart is only allowed after a bust.");

            StartNewRun();
            RunRestarted?.Invoke();
        }

        /// <summary>
        /// Development helper: moves straight to <paramref name="zone"/> without playing the earlier
        /// zones, keeping the current wallet. Ignored while a spin is animating. Used by the demo
        /// teleport keys; has no effect on shipping gameplay.
        /// </summary>
        public void JumpToZone(int zone)
        {
            if (Phase == SessionPhase.Spinning)
                return;

            if (zone < 1) zone = 1;
            if (_finalZone > 0 && zone > _finalZone) zone = _finalZone;

            PendingSliceIndex = -1;
            Zone = zone;
            Phase = SessionPhase.Idle;
            BuildWheel();
            StateChanged?.Invoke();
        }

        /// <summary>Resumes the run after a bomb without losing the current zone.</summary>
        public void Revive()
        {
            if (!CanRevive)
                throw new InvalidOperationException("Revive is only allowed after a bust.");

            PendingSliceIndex = -1;
            Phase = SessionPhase.Idle;
            BuildWheel();
            StateChanged?.Invoke();
        }
    }
}
