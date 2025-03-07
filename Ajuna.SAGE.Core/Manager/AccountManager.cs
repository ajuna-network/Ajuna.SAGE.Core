using Ajuna.SAGE.Core.Model;
using System.Collections.Generic;

namespace Ajuna.SAGE.Core.Manager
{
    public interface IAccountManager
    {
        uint Create();

        bool Destroy(uint id);

        IAccount? Account(uint id);

        uint EngineId { get; }
    }

    public class AccountManager : IAccountManager
    {
        private uint _nextId;

        private readonly Dictionary<uint, IAccount> _data = new Dictionary<uint, IAccount>();

        private readonly uint _engineId;
        public uint EngineId => _engineId;

        public AccountManager()
        {
            _nextId = 10000000;
            _data = new Dictionary<uint, IAccount>();

            _engineId = Create();
        }

        public uint Create()
        {
            uint id = _nextId++;
            _data.Add(id, new Account(id, 0));
            return id;
        }

        public bool Destroy(uint id)
        {
            if (!_data.ContainsKey(id))
            {
                return false;
            }
            _data.Remove(id);
            return true;
        }

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