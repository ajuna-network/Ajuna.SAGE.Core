using Ajuna.SAGE.Core.Model;
using System.Collections.Generic;

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

    public class AccountManager : IAccountManager
    {
        private uint _nextId;

        private readonly Dictionary<uint, IAccount> _data = new Dictionary<uint, IAccount>();

        private readonly uint _engineId;

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
    }
}
