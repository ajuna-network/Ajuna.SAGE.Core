using Ajuna.SAGE.Core.Manager;
using Ajuna.SAGE.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Ajuna.SAGE.Game.Test")]

namespace Ajuna.SAGE.Core
{
    public delegate IEnumerable<IAsset> TransitionFunction<TRules>(
        IAccount executor,
        TRules[] rules,
        ITransitionFee? fee,
        IEnumerable<IAsset> assets,
        byte[] randomHash,
        uint blockNumber,
        object? config,
        IBalanceManager assetBalances,
        ILock lockManager)
        where TRules : ITransitionRule;

    public class Engine<TIdentifier, TRules>
         where TIdentifier : ITransitionIdentifier
         where TRules : ITransitionRule
    {
        private readonly IBlockchainInfoProvider _blockchainInfo;
        public IBlockchainInfoProvider BlockchainInfoProvider => _blockchainInfo;

        private readonly Func<IAccount, TRules, IAsset[], uint, object?, IBalanceManager, IAssetManager, bool> _verifyFunction;

        private readonly Dictionary<TIdentifier, (TRules[] Rules, ITransitionFee? fee, TransitionFunction<TRules> Function)> _transitions;

        private readonly AccountManager _accountManager;
        public IAccountManager AccountManager => _accountManager;

        private readonly AssetManager _assetManager;
        public IAssetManager AssetManager => _assetManager;

        private readonly BalanceManager _assetBalanceManager;
        public IBalanceManager AssetBalanceManager => _assetBalanceManager;

        private readonly LockManager _lockManager;
        public ILock LockManager => _lockManager;

        // only for testing
        public uint? AssetBalance(ulong id) => _assetBalanceManager.AssetBalance(id);

        /// <summary>
        /// Game
        /// </summary>
        /// <param name="seed"></param>
        public Engine(IBlockchainInfoProvider blockchainInfo, Func<IAccount, TRules, IAsset[], uint, object?, IBalanceManager, IAssetManager, bool> verifyFunction)
        {
            _blockchainInfo = blockchainInfo;
            _verifyFunction = verifyFunction;
            _transitions = new Dictionary<TIdentifier, (TRules[] Rules, ITransitionFee? fee, TransitionFunction<TRules> Function)>();

            _accountManager = new AccountManager();
            _assetManager = new AssetManager();
            _assetBalanceManager = new BalanceManager();
            _lockManager = new LockManager();
        }

        /// <summary>
        /// Blockchain Info Provider
        /// </summary>
        public IBlockchainInfoProvider BlockchainInfo => _blockchainInfo;

        /// <summary>
        ///
        /// </summary>
        /// <param name="idType1"></param>
        /// <param name="idType2"></param>
        /// <param name="transitionFunction"></param>
        public void AddTransition(TIdentifier identifier, TRules[] rules, ITransitionFee? fee, TransitionFunction<TRules> function)
        {
            _transitions[identifier] = (rules, fee, function);
        }

        /// <summary>
        /// Transition
        /// </summary>
        /// <param name="avatars"></param>
        /// <param name="blockNumber"></param>
        /// <returns></returns>
        public bool Transition(IAccount executor, TIdentifier identifier, IAsset[]? avatars, out IAsset[] result, object? config = null)
        {
            return Transition(executor, identifier, avatars, _blockchainInfo.GenerateRandomHash(), _blockchainInfo.CurrentBlockNumber, out result, config);
        }

        /// <summary>
        /// Transition
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="inAssets"></param>
        /// <param name="randomHash"></param>
        /// <param name="blockNumber"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        internal bool Transition(IAccount executor, TIdentifier identifier, IAsset[]? inAssets, byte[] randomHash, uint blockNumber, out IAsset[] outAssets, object? config = null)
        {
            // initialize to avoid null checks
            inAssets ??= Array.Empty<IAsset>();

            // duplicate check
            if (inAssets.Distinct().Count() != inAssets.Length)
            {
                throw new NotSupportedException("Trying to transition duplicates.");
            }

            // lock check: reject assets that are lockable AND currently locked
            if (inAssets.Any(p => p.IsLockable && _lockManager.IsLocked(p.Id) == true))
            {
                throw new NotSupportedException("Trying to transition a locked asset.");
            }

            if (!_transitions.TryGetValue(identifier, out (TRules[] rules, ITransitionFee? fee, TransitionFunction<TRules> function) tuple))
            {
                throw new NotSupportedException($"Unsupported Transition for Identifier ({identifier.TransitionType}, {identifier.TransitionSubType}).");
            }

            TRules[] rules = tuple.rules;
            ITransitionFee? fee = tuple.fee;
            TransitionFunction<TRules> function = tuple.function;

            // check if the executor has the assets and the rules are all okay
            if (!rules.All(rule => _verifyFunction(executor, rule, inAssets, blockNumber, config, _assetBalanceManager, _assetManager)))
            {
                outAssets = Array.Empty<IAsset>();
                return false;
            }

            // check if the executor has enough balance to pay the fee
            if (fee != null && fee.Fee > 0 && !executor.Balance.Withdraw(fee.Fee))
            {
                outAssets = Array.Empty<IAsset>();
                return false;
            }

            // execute the transition function
            IEnumerable<IAsset> functionResult = function(executor, rules, fee, inAssets, randomHash, blockNumber, config, _assetBalanceManager, _lockManager);

            outAssets = functionResult != null ? functionResult.ToArray() : Array.Empty<IAsset>();

            Transition(inAssets, outAssets);

            return true;
        }

        /// <summary>
        /// Transition
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="outputs"></param>
        internal void Transition(IAsset[]? inputs, IAsset[]? outputs)
        {
            var inputIds = inputs?.Select(a => a.Id).ToHashSet();
            var outputIds = outputs?.Select(a => a.Id).ToHashSet();
            var updateIds = new List<uint>();
            var deleteIds = new List<uint>();
            var createIds = new List<uint>();

            // find all updated and deleted ids
            if (inputIds != null)
            {
                foreach (var inputId in inputIds)
                {
                    if (outputIds != null && outputIds.Contains(inputId))
                    {
                        updateIds.Add(inputId);
                    }
                    else
                    {
                        deleteIds.Add(inputId);
                    }
                }
            }

            // find all created ids
            if (outputs != null)
            {
                foreach (var output in outputIds)
                {
                    if (inputIds == null || !inputIds.Contains(output))
                    {
                        createIds.Add(output);
                    }
                }
            }

            // handle all created assets in the asset manager
            foreach (var id in createIds)
            {
                _assetManager.Create(outputs.First(p => p.Id == id));
            }

            // handle all updated assets in the asset manager
            foreach (var id in updateIds)
            {
                _assetManager.Update(outputs.First(p => p.Id == id));
            }

            // handle all deleted assets in the asset manager
            foreach (var id in deleteIds)
            {
                _assetManager.Delete(id);
            }
        }

    }
}
