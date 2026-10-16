using System;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>A weighted reward entry inside a <see cref="RewardPool"/>.</summary>
    [Serializable]
    public struct RewardPoolItem
    {
        public RewardDefinition reward;
        [Min(0f)] public float weight;
    }

    /// <summary>
    /// Editor-configurable weighted reward pool. Weights control how often each reward is
    /// selected when a wheel is built.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Reward Pool", fileName = "Pool_")]
    public sealed class RewardPool : ScriptableObject
    {
        [SerializeField] private RewardPoolItem[] entries = new RewardPoolItem[0];

        public RewardPoolItem[] Entries => entries;
    }
}
