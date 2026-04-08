using Ajuna.SAGE.Core.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ajuna.SAGE.Core.Manager
{
    /// <summary>
    /// Manages game assets (CRUD operations and ownership queries).
    /// Assets are the fundamental game objects with data, score, and ownership.
    /// </summary>
    public interface IAssetManager
    {
        /// <summary>
        /// Create a new asset. Assigns a unique ID and stores it. Returns the assigned ID.
        /// </summary>
        uint Create(IAsset asset);

        /// <summary>
        /// Read an asset by ID. Returns null if not found.
        /// </summary>
        IAsset? Read(uint id);

        /// <summary>
        /// Update an existing asset. Replaces the stored asset with the given one.
        /// Returns false if the asset does not exist.
        /// </summary>
        bool Update(IAsset asset);

        /// <summary>
        /// Delete an asset by reference. Returns false if not found.
        /// </summary>
        bool Delete(IAsset asset);

        /// <summary>
        /// Delete an asset by ID. Returns false if not found.
        /// </summary>
        bool Delete(uint id);

        /// <summary>
        /// Get all assets owned by the given account.
        /// </summary>
        IEnumerable<IAsset> AssetOf(IAccount account);

        /// <summary>
        /// Enumerate every asset currently in the manager regardless of
        /// owner. Order is not guaranteed.
        /// </summary>
        IEnumerable<IAsset> AllAssets();
    }

    public class AssetManager : IAssetManager, ISnapshotable
    {
        // Snapshot format version for AssetManager. Bump on layout change.
        private const byte SNAPSHOT_VERSION = 1;

        private uint _nextId;

        private readonly Dictionary<uint, IAsset> _data;

        public AssetManager()
        {
            _nextId = 1000;
            _data = new Dictionary<uint, IAsset>();
        }

        /// <inheritdoc/>
        public uint Create(IAsset asset)
        {
            uint id = _nextId++;
            asset.Id = id;
            _data.Add(id, asset);
            return id;
        }

        /// <inheritdoc/>
        public IAsset? Read(uint id)
        {
            if (!_data.TryGetValue(id, out IAsset? asset))
            {
                return null;
            }
            return asset;
        }

        /// <inheritdoc/>
        public bool Update(IAsset asset)
        {
            if (!_data.Remove(asset.Id))
            {
                return false;
            }

            _data.Add(asset.Id, asset);
            return true;
        }

        /// <inheritdoc/>
        public bool Delete(IAsset asset)
        {
            return Delete(asset.Id);
        }

        /// <inheritdoc/>
        public bool Delete(uint id)
        {
            return _data.Remove(id);
        }

        /// <inheritdoc/>
        public IEnumerable<IAsset> AssetOf(IAccount account)
        {
            return _data.Values.Where(p => p.OwnedBy(account)).ToList();
        }

        /// <inheritdoc/>
        public IEnumerable<IAsset> AllAssets() => _data.Values.ToList();

        /// <inheritdoc/>
        public byte[] Snapshot()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(SNAPSHOT_VERSION);
            w.Write(_nextId);
            w.Write(_data.Count);
            foreach (var asset in _data.Values)
            {
                w.Write(asset.Id);
                w.Write(asset.OwnerId);
                w.Write(asset.CollectionId);
                w.Write(asset.Score);
                w.Write(asset.Genesis);
                w.Write(asset.IsLockable);
                w.Write(asset.MatchTypeSize);
                if (asset.Data == null)
                {
                    w.Write(-1);
                }
                else
                {
                    w.Write(asset.Data.Length);
                    w.Write(asset.Data);
                }
            }
            return ms.ToArray();
        }

        /// <inheritdoc/>
        public void Restore(byte[] snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("AssetManager snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != SNAPSHOT_VERSION)
                throw new InvalidDataException($"AssetManager snapshot version {version} not supported (expected {SNAPSHOT_VERSION})");

            _data.Clear();
            _nextId = r.ReadUInt32();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                uint id = r.ReadUInt32();
                uint ownerId = r.ReadUInt32();
                byte collectionId = r.ReadByte();
                uint score = r.ReadUInt32();
                uint genesis = r.ReadUInt32();
                bool isLockable = r.ReadBoolean();
                byte matchTypeSize = r.ReadByte();
                int dataLen = r.ReadInt32();
                byte[]? data = dataLen < 0 ? null : r.ReadBytes(dataLen);

                var asset = new Asset(id, ownerId, collectionId, score, genesis, data)
                {
                    IsLockable = isLockable,
                    MatchTypeSize = matchTypeSize,
                };
                _data[id] = asset;
            }
        }
    }
}
