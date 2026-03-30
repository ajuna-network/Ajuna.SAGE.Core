using Ajuna.SAGE.Core.Model;
using System.Collections.Generic;
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
    }

    public class AssetManager : IAssetManager
    {
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
    }
}
