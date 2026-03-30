using System.Collections.Generic;
using System.Linq;

namespace Ajuna.SAGE.Core.Manager
{
    /// <summary>
    /// Manages token balances associated with assets.
    /// Each asset can hold a balance identified by its asset ID.
    /// </summary>
    public interface IBalanceManager
    {
        /// <summary>
        /// Check if a deposit would succeed without overflow.
        /// Returns the current balance via out parameter.
        /// </summary>
        bool CanDeposit(ulong id, uint balance, out uint currentBalance);

        /// <summary>
        /// Deposit tokens into an asset's balance.
        /// Returns false if the deposit would overflow.
        /// </summary>
        bool Deposit(ulong id, uint balance);

        /// <summary>
        /// Check if a withdrawal would succeed (sufficient balance).
        /// Returns the current balance via out parameter.
        /// </summary>
        bool CanWithdraw(ulong id, uint balance, out uint currentBalance);

        /// <summary>
        /// Withdraw tokens from an asset's balance.
        /// Returns false if the asset has insufficient balance.
        /// </summary>
        bool Withdraw(ulong id, uint balance);

        /// <summary>
        /// Get the sum of all asset balances.
        /// </summary>
        ulong AllAssetBalances();

        /// <summary>
        /// Get the balance for a specific asset. Returns null if no balance exists.
        /// </summary>
        uint? AssetBalance(ulong id);
    }

    public class BalanceManager : IBalanceManager
    {
        private readonly Dictionary<ulong, uint> _data = new Dictionary<ulong, uint>();

        /// <inheritdoc/>
        public bool CanDeposit(ulong id, uint balance, out uint currentBalance)
        {
            return !_data.TryGetValue(id, out currentBalance) || balance <= uint.MaxValue - currentBalance;
        }

        /// <inheritdoc/>
        public bool Deposit(ulong id, uint balance)
        {
            if (!CanDeposit(id, balance, out uint currentBalance))
            {
                return false;
            }
            _data[id] = currentBalance + balance;
            return true;
        }

        /// <inheritdoc/>
        public bool CanWithdraw(ulong id, uint balance, out uint currentBalance)
        {
            return _data.TryGetValue(id, out currentBalance) && balance <= currentBalance;
        }

        /// <inheritdoc/>
        public bool Withdraw(ulong id, uint balance)
        {
            if (!CanWithdraw(id, balance, out uint currentBalance))
            {
                return false;
            }

            _data[id] = currentBalance - balance;
            return true;
        }

        /// <inheritdoc/>
        public ulong AllAssetBalances() => (ulong)_data.Sum(x => x.Value);

        /// <inheritdoc/>
        public uint? AssetBalance(ulong id) => _data.TryGetValue(id, out uint balance) ? balance : (uint?)null;
    }
}
