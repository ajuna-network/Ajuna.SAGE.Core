using System.Collections.Generic;
using System.IO;
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

    public class BalanceManager : IBalanceManager, ISnapshotable
    {
        // Snapshot format version for BalanceManager. Bump on layout change.
        private const byte SNAPSHOT_VERSION = 1;

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

        /// <inheritdoc/>
        public byte[] Snapshot()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(SNAPSHOT_VERSION);
            w.Write(_data.Count);
            foreach (var kv in _data)
            {
                w.Write(kv.Key);
                w.Write(kv.Value);
            }
            return ms.ToArray();
        }

        /// <inheritdoc/>
        public void Restore(byte[] snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("BalanceManager snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != SNAPSHOT_VERSION)
                throw new InvalidDataException($"BalanceManager snapshot version {version} not supported (expected {SNAPSHOT_VERSION})");

            _data.Clear();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ulong key = r.ReadUInt64();
                uint value = r.ReadUInt32();
                _data[key] = value;
            }
        }
    }
}
