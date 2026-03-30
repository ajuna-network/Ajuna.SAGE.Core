using System.Collections.Generic;

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
    }

    public class LockManager : ILock
    {
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
    }
}
