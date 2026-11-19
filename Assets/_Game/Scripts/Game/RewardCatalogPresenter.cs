using System.Collections.Generic;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>Session-local content lookup and reusable reward-row projection, not a flow owner.</summary>
    public sealed class RewardCatalogPresenter : IRewardCurrencyResolver
    {
        private readonly Dictionary<string, RewardDefinition> _definitions = new Dictionary<string, RewardDefinition>();
        private readonly List<RewardRowData> _rows = new List<RewardRowData>();

        public void Configure(ZoneConfiguration configuration)
        {
            _definitions.Clear();
            foreach (WheelDefinition wheel in configuration.AllWheels)
            {
                if (!wheel) continue;
                AddPool(wheel.RewardPool);
                foreach (var band in wheel.RewardPoolBands) AddPool(band.Pool);
            }
        }

        private void AddPool(RewardPool pool)
        {
            if (!pool) return;
            foreach (var entry in pool.Entries)
            {
                if (entry.reward) _definitions[entry.reward.RewardId] = entry.reward;
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

        public List<RewardRowData> BuildRows(RewardWallet wallet)
        {
            _rows.Clear();
            foreach (var pair in wallet.Totals)
            {
                RewardDefinition definition = Resolve(pair.Key);
                int rank = CategoryRank(definition);
                int insertionIndex = _rows.Count;
                while (insertionIndex > 0 && CategoryRank(Resolve(_rows[insertionIndex - 1].Id)) > rank)
                {
                    insertionIndex--;
                }
                _rows.Insert(insertionIndex, new RewardRowData(pair.Key,
                    definition ? definition.Icon : null,
                    definition ? definition.DisplayName : pair.Key, pair.Value));
            }
            return _rows;
        }

        private static int CategoryRank(RewardDefinition definition)
        {
            if (!definition || definition.Category == RewardCategory.Uncategorized) return int.MaxValue;
            return (int)definition.Category;
        }
    }
}
