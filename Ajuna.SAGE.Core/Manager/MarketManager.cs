using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ajuna.SAGE.Core.Manager
{
    /// <summary>
    /// Manages asset marketplace listings.
    /// Tracks which assets are listed for sale and at what price.
    /// </summary>
    public interface IMarket
    {
        /// <summary>
        /// List an asset for sale at the given price. Fails if already listed.
        /// </summary>
        bool List(uint assetId, uint price);

        /// <summary>
        /// Remove an asset from the marketplace. Fails if not listed.
        /// </summary>
        bool Delist(uint assetId);

        /// <summary>
        /// Get the listing price. Returns null if not listed.
        /// </summary>
        uint? GetPrice(uint assetId);

        /// <summary>
        /// Check if an asset is currently listed for sale.
        /// </summary>
        bool IsListed(uint assetId);

        /// <summary>
        /// Get all currently listed asset IDs with their prices.
        /// </summary>
        IEnumerable<(uint assetId, uint price)> GetAllListings();
    }

    public class MarketManager : IMarket, ISnapshotable
    {
        // Snapshot format version for MarketManager. Bump on layout change.
        private const byte SNAPSHOT_VERSION = 1;

        private readonly Dictionary<uint, uint> _listings = new Dictionary<uint, uint>();

        public bool List(uint assetId, uint price)
        {
            if (price == 0 || _listings.ContainsKey(assetId))
            {
                return false;
            }

            _listings[assetId] = price;
            return true;
        }

        public bool Delist(uint assetId)
        {
            return _listings.Remove(assetId);
        }

        public uint? GetPrice(uint assetId)
        {
            return _listings.TryGetValue(assetId, out uint price) ? price : (uint?)null;
        }

        public bool IsListed(uint assetId)
        {
            return _listings.ContainsKey(assetId);
        }

        public IEnumerable<(uint assetId, uint price)> GetAllListings()
        {
            return _listings.Select(kv => (kv.Key, kv.Value));
        }

        /// <inheritdoc/>
        public byte[] Snapshot()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(SNAPSHOT_VERSION);
            w.Write(_listings.Count);
            foreach (var kv in _listings)
            {
                w.Write(kv.Key);
                w.Write(kv.Value);
            }
            return ms.ToArray();
        }

        /// <inheritdoc/>
        public void Restore(byte[] snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("MarketManager snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != SNAPSHOT_VERSION)
                throw new InvalidDataException($"MarketManager snapshot version {version} not supported (expected {SNAPSHOT_VERSION})");

            _listings.Clear();
            int count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                uint key = r.ReadUInt32();
                uint value = r.ReadUInt32();
                _listings[key] = value;
            }
        }
    }
}
