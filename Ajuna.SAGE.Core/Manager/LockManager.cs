using System.Collections.Generic;
using System.IO;

namespace Ajuna.SAGE.Core.Manager
{
    /// <summary>
    /// Manages lock state for assets.
    /// An asset must have IsLockable=true to support locking.
    /// The engine rejects transitions on assets that are lockable AND currently locked.
    /// </summary>
    public interface ILock
    {
        /// <summary>
        /// Check if an asset can be locked (not already locked).
        /// Returns the current lock state via out parameter.
        /// </summary>
        bool CanLock(ulong id, out bool lockState);

        /// <summary>
        /// Lock an asset. Returns false if already locked.
        /// </summary>
        bool Lock(ulong id);

        /// <summary>
        /// Check if an asset can be unlocked (currently locked).
        /// Returns the current lock state via out parameter.
        /// </summary>
        bool CanUnlock(ulong id, out bool lockState);

        /// <summary>
        /// Unlock an asset. Returns false if not currently locked.
        /// </summary>
        bool Unlock(ulong id);

        /// <summary>
        /// Inspect the current lock state for an asset without mutating it.
        /// Returns null if the asset has never been locked or unlocked.
        /// </summary>
        bool? IsLocked(ulong id);
    }

    public class LockManager : ILock, ISnapshotable
    {
        // Snapshot format version for LockManager. Bump on layout change.
        private const byte SNAPSHOT_VERSION = 1;

        private readonly Dictionary<ulong, bool> _data = new Dictionary<ulong, bool>();

        /// <inheritdoc/>
        public bool CanLock(ulong id, out bool state)
        {
            return !_data.TryGetValue(id, out state) || !state;
        }

        /// <inheritdoc/>
        public bool Lock(ulong id)
        {
            if (!CanLock(id, out _))
            {
                return false;
            }
            _data[id] = true;
            return true;
        }

        /// <inheritdoc/>
        public bool CanUnlock(ulong id, out bool state)
        {
            return _data.TryGetValue(id, out state) && state;
        }

        /// <inheritdoc/>
        public bool Unlock(ulong id)
        {
            if (!CanUnlock(id, out _))
            {
                return false;
            }

            _data[id] = false;

            return true;
        }

        /// <summary>
        /// Check if an asset is currently locked.
        /// Returns null if the asset has never been locked/unlocked.
        /// </summary>
        public bool? IsLocked(ulong id) => _data.TryGetValue(id, out bool state) ? state : (bool?)null;

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
            if (snapshot == null) throw new InvalidDataException("LockManager snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != SNAPSHOT_VERSION)
                throw new InvalidDataException($"LockManager snapshot version {version} not supported (expected {SNAPSHOT_VERSION})");

            _data.Clear();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                ulong key = r.ReadUInt64();
                bool value = r.ReadBoolean();
                _data[key] = value;
            }
        }
    }
}
