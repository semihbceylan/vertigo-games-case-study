using System;
using System.Globalization;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>Local demo persistence, not an authoritative or tamper-resistant economy backend.</summary>
    public sealed class PlayerPrefsCurrencyStore : ICurrencyStore
    {
        #region Persistence

        private const string KEY_PREFIX = "wheel.currency.v1.";

        public long Load(string currencyId)
        {
            string key = KEY_PREFIX + currencyId;
            if (!PlayerPrefs.HasKey(key))
            {
                return 0;
            }
            if (!long.TryParse(PlayerPrefs.GetString(key), NumberStyles.None,
                CultureInfo.InvariantCulture, out long balance) || balance < 0)
            {
                throw new InvalidOperationException("Invalid persisted currency balance: " + currencyId);
            }
            return balance;
        }

        public void Save(string currencyId, long balance)
        {
            if (balance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(balance));
            }
            PlayerPrefs.SetString(KEY_PREFIX + currencyId, balance.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        #endregion
    }
}
