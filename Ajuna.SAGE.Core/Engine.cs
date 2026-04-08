using Ajuna.SAGE.Core.Manager;
using Ajuna.SAGE.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
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
        ILock lockManager,
        IMarket marketManager)
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

        private readonly MarketManager _marketManager;
        public IMarket MarketManager => _marketManager;

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
            _marketManager = new MarketManager();
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
            IEnumerable<IAsset> functionResult = function(executor, rules, fee, inAssets, randomHash, blockNumber, config, _assetBalanceManager, _lockManager, _marketManager);

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

        // ─── Snapshot / Restore facade ────────────────────────────────────
        //
        // Engine.Snapshot() and Engine.Restore() let consumers persist the
        // entire engine state via a single opaque byte payload. Each
        // manager (and the BlockchainInfoProvider) implements ISnapshotable;
        // the engine simply concatenates each component's snapshot inside a
        // versioned envelope so future format additions don't break old
        // snapshots.
        //
        // Transition rules and the verify function are NOT serialized — they
        // are wired in at engine construction by the consumer's builder, so
        // a typical restore flow looks like:
        //
        //     var engine = AvatarEngineBuilder.Build(blockchainInfo);
        //     engine.Restore(File.ReadAllBytes("gamestate.bin"));
        //
        // The consumer is responsible for using the SAME builder both before
        // saving and after loading.

        private const byte ENGINE_SNAPSHOT_VERSION = 1;

        /// <summary>
        /// Serializes the entire engine state (all five managers + the
        /// blockchain info provider) into a single versioned byte payload
        /// that can later be replayed via <see cref="Restore"/>.
        /// </summary>
        public byte[] Snapshot()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);

            w.Write(ENGINE_SNAPSHOT_VERSION);

            WriteSection(w, _accountManager.Snapshot());
            WriteSection(w, _assetManager.Snapshot());
            WriteSection(w, _assetBalanceManager.Snapshot());
            WriteSection(w, _lockManager.Snapshot());
            WriteSection(w, _marketManager.Snapshot());
            WriteSection(w, RequireSnapshotable(_blockchainInfo, nameof(_blockchainInfo)).Snapshot());

            return ms.ToArray();
        }

        /// <summary>
        /// Replaces the entire engine state with the contents of a snapshot
        /// previously produced by <see cref="Snapshot"/>. Existing in-memory
        /// state is discarded before the snapshot is applied. Throws
        /// <see cref="InvalidDataException"/> if the payload is malformed
        /// or its envelope version is incompatible.
        /// </summary>
        public void Restore(byte[] snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("Engine snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);

            var version = r.ReadByte();
            if (version != ENGINE_SNAPSHOT_VERSION)
                throw new InvalidDataException($"Engine snapshot version {version} not supported (expected {ENGINE_SNAPSHOT_VERSION})");

            _accountManager.Restore(ReadSection(r));
            _assetManager.Restore(ReadSection(r));
            _assetBalanceManager.Restore(ReadSection(r));
            _lockManager.Restore(ReadSection(r));
            _marketManager.Restore(ReadSection(r));
            RequireSnapshotable(_blockchainInfo, nameof(_blockchainInfo)).Restore(ReadSection(r));
        }

        private static void WriteSection(BinaryWriter w, byte[] payload)
        {
            w.Write(payload.Length);
            w.Write(payload);
        }

        private static byte[] ReadSection(BinaryReader r)
        {
            int length = r.ReadInt32();
            return r.ReadBytes(length);
        }

        private static ISnapshotable RequireSnapshotable(object component, string name)
        {
            if (component is ISnapshotable s) return s;
            throw new InvalidOperationException(
                $"{name} ({component.GetType().FullName}) does not implement ISnapshotable; cannot snapshot/restore the engine.");
        }
    }
}
