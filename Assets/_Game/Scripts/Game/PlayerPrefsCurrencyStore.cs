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

        /// <summary>v2 introduced starting balances; v1 saves are ignored so every player receives them.</summary>
        private const string KEY_PREFIX = "wheel.currency.v2.";

        public bool TryLoad(string currencyId, out long balance)
        {
            string key = KEY_PREFIX + currencyId;
            if (!PlayerPrefs.HasKey(key))
            {
                balance = 0;
                return false;
            }
            if (!long.TryParse(PlayerPrefs.GetString(key), NumberStyles.None,
                CultureInfo.InvariantCulture, out balance) || balance < 0)
            {
                throw new InvalidOperationException("Invalid persisted currency balance: " + currencyId);
            }
            return true;
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
