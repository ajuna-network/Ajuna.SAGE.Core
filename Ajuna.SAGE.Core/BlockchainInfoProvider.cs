
using System;
using System.IO;

namespace Ajuna.SAGE.Core
{
    public interface IBlockchainInfoProvider
    {
        byte[] GenerateRandomHash();
        uint CurrentBlockNumber { get; set; }
        void IncrementBlockNumber();
    }

    public class BlockchainInfoProvider : IBlockchainInfoProvider, ISnapshotable
    {
        // Snapshot format version for BlockchainInfoProvider.
        private const byte SNAPSHOT_VERSION = 1;

        private int _seed;
        private uint _hashCount;
        private Random _random;

        public BlockchainInfoProvider(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
        }

        public byte[] GenerateRandomHash()
        {
            byte[] randomHash = new byte[32];
            _random.NextBytes(randomHash);
            _hashCount++;
            return randomHash;
        }

        public uint CurrentBlockNumber { get; set; } = 1;

        public void IncrementBlockNumber()
        {
            CurrentBlockNumber++;
        }

        /// <inheritdoc/>
        public byte[] Snapshot()
        {
            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);
            w.Write(SNAPSHOT_VERSION);
            w.Write(_seed);
            w.Write(_hashCount);
            w.Write(CurrentBlockNumber);
            return ms.ToArray();
        }

        /// <inheritdoc/>
        public void Restore(byte[] snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("BlockchainInfoProvider snapshot is null");
            using var ms = new MemoryStream(snapshot);
            using var r = new BinaryReader(ms);
            var version = r.ReadByte();
            if (version != SNAPSHOT_VERSION)
                throw new InvalidDataException($"BlockchainInfoProvider snapshot version {version} not supported (expected {SNAPSHOT_VERSION})");

            _seed = r.ReadInt32();
            _hashCount = r.ReadUInt32();
            CurrentBlockNumber = r.ReadUInt32();

            // Recreate the RNG from the original seed and re-advance it
            // by _hashCount calls so that subsequent GenerateRandomHash()
            // calls produce the same sequence as before the snapshot.
            _random = new Random(_seed);
            var scratch = new byte[32];
            for (uint i = 0; i < _hashCount; i++)
            {
                _random.NextBytes(scratch);
            }
        }
    }
}