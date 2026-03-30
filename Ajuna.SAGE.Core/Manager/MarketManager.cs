using System.Collections.Generic;
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

    public class MarketManager : IMarket
    {
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
    }
}
