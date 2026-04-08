using Ajuna.SAGE.Core.Model;
using System.Collections.Generic;
using System.IO;

namespace Ajuna.SAGE.Core.Manager
{
    /// <summary>
    /// Manages player and system accounts.
    /// Each account has a unique ID and a balance.
    /// </summary>
    public interface IAccountManager
    {
        /// <summary>
        /// Create a new account with zero balance. Returns the new account ID.
        /// </summary>
        uint Create();

        /// <summary>
        /// Destroy an account by ID. Returns false if the account does not exist.
        /// </summary>
        bool Destroy(uint id);

        /// <summary>
        /// Look up an account by ID. Returns null if not found.
        /// </summary>
        IAccount? Account(uint id);

        /// <summary>
        /// The engine's own account ID, automatically created at construction.
        /// Used as the admin/system account.
        /// </summary>
        uint EngineId { get; }
    }

    public class AccountManager : IAccountManager, ISnapshotable
    {
        // Snapshot format version for AccountManager. Bump on layout change.
        private const byte SNAPSHOT_VERSION = 1;

        private uint _nextId;

        private readonly Dictionary<uint, IAccount> _data = new Dictionary<uint, IAccount>();

        private uint _engineId;

        /// <inheritdoc/>
        public uint EngineId => _engineId;

        public AccountManager()
        {
            _nextId = 10000000;
            _data = new Dictionary<uint, IAccount>();

            _engineId = Create();
        }

        /// <inheritdoc/>
        public uint Create()
        {
            uint id = _nextId++;
            _data.Add(id, new Account(id, 0));
            return id;
        }

        /// <inheritdoc/>
        public bool Destroy(uint id)
        {
            if (!_data.ContainsKey(id))
            {
                return false;
            }
            _data.Remove(id);
            return true;
        }

        /// <inheritdoc/>
        public IAccount? Account(uint id)
        {
            if (!_data.TryGetValue(id, out IAccount? account))
            {
                return null;
            }
            return account;
        }

        /// <inheritdoc/>
        public byte[] Snapshot()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(SNAPSHOT_VERSION);
            w.Write(_nextId);
            w.Write(_engineId);
            w.Write(_data.Count);
            foreach (var kv in _data)
            {
                w.Write(kv.Value.Id);
                w.Write(kv.Value.Balance.Value);
            }
            return ms.ToArray();
        }

        /// <inheritdoc/>
        public void Restore(byte[] snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("AccountManager snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != SNAPSHOT_VERSION)
                throw new InvalidDataException($"AccountManager snapshot version {version} not supported (expected {SNAPSHOT_VERSION})");

            _data.Clear();
            _nextId = r.ReadUInt32();
            _engineId = r.ReadUInt32();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                uint id = r.ReadUInt32();
                uint balance = r.ReadUInt32();
                _data[id] = new Account(id, balance);
            }
        }
    }
}
