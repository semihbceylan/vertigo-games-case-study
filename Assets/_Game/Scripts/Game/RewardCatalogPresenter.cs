using System;
using System.Collections.Generic;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>Session-local content lookup and reusable reward-row projection, not a flow owner.</summary>
    public sealed class RewardCatalogPresenter : IRewardCurrencyResolver
    {
        #region Catalogue

        private readonly Dictionary<string, RewardDefinition> _definitions = new Dictionary<string, RewardDefinition>();
        private readonly List<RewardRowData> _rows = new List<RewardRowData>();

        public void Configure(ZoneConfiguration configuration)
        {
            _definitions.Clear();
            foreach (WheelDefinition wheel in configuration.AllWheels)
            {
                AddPool(wheel.RewardPool);
                foreach (RewardPoolBand band in wheel.RewardPoolBands)
                {
                    AddPool(band.Pool);
                }
            }
        }

        private void AddPool(RewardPool pool)
        {
            if (!pool)
            {
                return;
            }
            foreach (RewardPoolItem entry in pool.Entries)
            {
                _definitions[entry.Reward.RewardId] = entry.Reward;
            }
        }

        public RewardDefinition Resolve(string rewardId)
        {
            RewardDefinition definition;
            return _definitions.TryGetValue(rewardId, out definition) ? definition : null;
        }

        public string ResolveCurrencyId(string rewardId)
        {
            RewardDefinition definition = Resolve(rewardId);
            return definition && definition.Currency ? definition.Currency.CurrencyId : null;
        }

        #endregion

        #region Row Projection

        public List<RewardRowData> BuildRows(RewardWallet wallet)
        {
            return BuildRowsFromTotals(wallet.Totals);
        }

        public List<RewardRowData> BuildRowsFromTotals(IReadOnlyDictionary<string, long> totals)
        {
            _rows.Clear();
            foreach (KeyValuePair<string, long> pair in totals)
            {
                RewardDefinition definition = ResolveRequired(pair.Key);
                int rank = definition.Category.SortOrder;
                int insertionIndex = _rows.Count;
                while (insertionIndex > 0 && ResolveRequired(_rows[insertionIndex - 1].Id).Category.SortOrder > rank)
                {
                    insertionIndex--;
                }
                _rows.Insert(insertionIndex, new RewardRowData(pair.Key, definition.Icon, definition.DisplayName, pair.Value));
            }
            return _rows;
        }

        /// <summary>Unknown reward IDs are invalid data and are rejected rather than displayed raw.</summary>
        public RewardDefinition ResolveRequired(string rewardId)
        {
            RewardDefinition definition = Resolve(rewardId);
            if (!definition)
            {
                throw new InvalidOperationException("Reward '" + rewardId + "' is not in the configured catalogue.");
            }
            return definition;
        }

        #endregion
    }
}
