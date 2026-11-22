using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>A weighted reward entry inside a <see cref="RewardPool"/>.</summary>
    [Serializable]
    public struct RewardPoolItem
    {
        [FormerlySerializedAs("reward")] public RewardDefinition Reward;
        [Min(0f), FormerlySerializedAs("weight")] public float Weight;
    }

    /// <summary>
    /// Editor-configurable weighted reward pool. Weights control how often each reward is
    /// selected when a wheel is built.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Reward Pool", fileName = "Pool_")]
    public sealed class RewardPool : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, FormerlySerializedAs("entries")] private RewardPoolItem[] _entries = new RewardPoolItem[0];

        #endregion

        #region Accessors

        public RewardPoolItem[] Entries
        {
            get { return _entries; }
        }

        #endregion

#if UNITY_EDITOR
        #region Validation

        private void OnValidate()
        {
            foreach (RewardPoolItem entry in _entries)
            {
                if (!entry.Reward)
                {
                    Debug.LogError("Reward pool contains an empty entry: " + name, this);
                }
            }
        }

        #endregion
#endif
    }
}
