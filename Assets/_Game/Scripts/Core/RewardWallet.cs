using System.Collections.Generic;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>
    /// Accumulates rewards gathered during a run. Holds only runtime state - configuration
    /// lives in ScriptableObjects. Cleared when a bomb is hit.
    /// </summary>
    public sealed class RewardWallet
    {
        #region Run Rewards

        private readonly List<RewardGrant> _grants = new List<RewardGrant>();
        private readonly Dictionary<string, long> _totals = new Dictionary<string, long>();


        public IReadOnlyList<RewardGrant> Grants { get { return _grants; } }
        public IReadOnlyDictionary<string, long> Totals { get { return _totals; } }
        public int Count { get { return _grants.Count; } }
        public bool IsEmpty { get { return _grants.Count == 0; } }
        public long AccumulatedAmount { get; private set; }

        public void Add(RewardGrant grant)
        {
            _grants.Add(grant);

            long current;
            _totals.TryGetValue(grant.RewardId, out current);
            _totals[grant.RewardId] = current + grant.Amount;
            AccumulatedAmount += grant.Amount;

        }

        public void Clear()
        {
            if (_grants.Count == 0 && AccumulatedAmount == 0)
            {
                return;
            }

            _grants.Clear();
            _totals.Clear();
            AccumulatedAmount = 0;
        }

        #endregion
    }
}
