using Ajuna.SAGE.Core.Manager;
using Ajuna.SAGE.Core.Model;
using System.IO;

namespace Ajuna.SAGE.Core.Test
{
    /// <summary>
    /// Round-trip tests for the new ISnapshotable surface on each manager
    /// and BlockchainInfoProvider, plus the Engine.Snapshot/Restore facade.
    /// </summary>
    [TestFixture]
    public class SnapshotTest
    {
        // ── AccountManager ────────────────────────────────────────────

        [Test]
        public void AccountManager_Snapshot_RoundTrip_PreservesAccountsAndIds()
        {
            var src = new AccountManager();
            var id1 = src.Create();
            var id2 = src.Create();
            src.Account(id1)!.Balance.Deposit(123);
            src.Account(id2)!.Balance.Deposit(456);

            var snapshot = src.Snapshot();

            var dst = new AccountManager();
            dst.Restore(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(dst.EngineId, Is.EqualTo(src.EngineId));
                Assert.That(dst.Account(id1), Is.Not.Null);
                Assert.That(dst.Account(id1)!.Balance.Value, Is.EqualTo(123));
                Assert.That(dst.Account(id2)!.Balance.Value, Is.EqualTo(456));
                // Next-id counter must continue from where the source left off.
                var idAfter = dst.Create();
                Assert.That(idAfter, Is.GreaterThan(id2));
            });
        }

        [Test]
        public void AccountManager_Restore_BadVersion_Throws()
        {
            var dst = new AccountManager();
            var bad = new byte[] { 99 }; // wrong version byte
            Assert.Throws<InvalidDataException>(() => dst.Restore(bad));
        }

        // ── AssetManager ──────────────────────────────────────────────

        [Test]
        public void AssetManager_Snapshot_RoundTrip_PreservesAssets()
        {
            var src = new AssetManager();
            var a1 = new Asset(0, 1, 7, 100, 200, new byte[] { 0xAA, 0xBB }) { IsLockable = true };
            var a2 = new Asset(0, 2, 7, 50, 0, new byte[] { 0x01 }) { IsLockable = false };
            src.Create(a1);
            src.Create(a2);

            var snapshot = src.Snapshot();

            var dst = new AssetManager();
            dst.Restore(snapshot);

            var restored1 = dst.Read(a1.Id);
            var restored2 = dst.Read(a2.Id);

            Assert.Multiple(() =>
            {
                Assert.That(restored1, Is.Not.Null);
                Assert.That(restored1!.OwnerId, Is.EqualTo(1));
                Assert.That(restored1.CollectionId, Is.EqualTo(7));
                Assert.That(restored1.Score, Is.EqualTo(100));
                Assert.That(restored1.Genesis, Is.EqualTo(200));
                Assert.That(restored1.IsLockable, Is.True);
                Assert.That(restored1.Data, Is.EqualTo(new byte[] { 0xAA, 0xBB }));
                Assert.That(restored2!.IsLockable, Is.False);
                // Next-id continues from the source's high-water mark.
                var newAsset = new Asset(0, 1, 1, 0, 0, new byte[1]);
                var newId = dst.Create(newAsset);
                Assert.That(newId, Is.GreaterThan(a2.Id));
            });
        }

        [Test]
        public void AssetManager_AllAssets_EnumeratesEverything()
        {
            var mgr = new AssetManager();
            mgr.Create(new Asset(0, 1, 1, 0, 0, new byte[1]));
            mgr.Create(new Asset(0, 2, 1, 0, 0, new byte[1]));
            mgr.Create(new Asset(0, 3, 1, 0, 0, new byte[1]));

            var all = mgr.AllAssets().ToList();
            Assert.That(all, Has.Count.EqualTo(3));
        }

        // ── BalanceManager ────────────────────────────────────────────

        [Test]
        public void BalanceManager_Snapshot_RoundTrip_PreservesBalances()
        {
            var src = new BalanceManager();
            src.Deposit(1, 1000);
            src.Deposit(42, 250);
            src.Deposit(1000000, 999_999);

            var snapshot = src.Snapshot();

            var dst = new BalanceManager();
            dst.Restore(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(dst.AssetBalance(1), Is.EqualTo(1000));
                Assert.That(dst.AssetBalance(42), Is.EqualTo(250));
                Assert.That(dst.AssetBalance(1000000), Is.EqualTo(999_999));
                Assert.That(dst.AssetBalance(99), Is.Null);
            });
        }

        // ── LockManager ───────────────────────────────────────────────

        [Test]
        public void LockManager_Snapshot_RoundTrip_PreservesLockStates()
        {
            var src = new LockManager();
            src.Lock(1);
            src.Lock(2);
            src.Lock(3);
            src.Unlock(2); // 2 ends in unlocked state, but tracked

            var snapshot = src.Snapshot();

            var dst = new LockManager();
            dst.Restore(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(dst.IsLocked(1), Is.True);
                Assert.That(dst.IsLocked(2), Is.False);
                Assert.That(dst.IsLocked(3), Is.True);
                Assert.That(dst.IsLocked(99), Is.Null);
            });
        }

        // ── MarketManager ─────────────────────────────────────────────

        [Test]
        public void MarketManager_Snapshot_RoundTrip_PreservesListings()
        {
            var src = new MarketManager();
            src.List(10, 500);
            src.List(20, 1000);
            src.List(30, 2500);

            var snapshot = src.Snapshot();

            var dst = new MarketManager();
            dst.Restore(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(dst.GetPrice(10), Is.EqualTo(500));
                Assert.That(dst.GetPrice(20), Is.EqualTo(1000));
                Assert.That(dst.GetPrice(30), Is.EqualTo(2500));
                Assert.That(dst.IsListed(99), Is.False);
            });
        }

        // ── BlockchainInfoProvider ────────────────────────────────────

        [Test]
        public void BlockchainInfoProvider_Snapshot_RoundTrip_PreservesBlockAndRng()
        {
            var src = new BlockchainInfoProvider(seed: 12345);
            src.IncrementBlockNumber();
            src.IncrementBlockNumber();
            src.IncrementBlockNumber();
            src.GenerateRandomHash();
            src.GenerateRandomHash();

            var snapshot = src.Snapshot();

            var dst = new BlockchainInfoProvider(seed: 99999); // wrong seed — restore must overwrite
            dst.Restore(snapshot);

            // Block number preserved
            Assert.That(dst.CurrentBlockNumber, Is.EqualTo(src.CurrentBlockNumber));

            // RNG continuation: next hash from src and from dst must match.
            var srcNext = src.GenerateRandomHash();
            var dstNext = dst.GenerateRandomHash();
            Assert.That(dstNext, Is.EqualTo(srcNext), "RNG should resume in the same position after restore");
        }

        // ── Engine facade ─────────────────────────────────────────────

        private static Engine<ActionIdentifier, ActionRule> NewEngine()
        {
            var bcp = new BlockchainInfoProvider(seed: 42);
            return new Engine<ActionIdentifier, ActionRule>(bcp, (p, r, a, b, c, m, s) => true);
        }

        [Test]
        public void Engine_Snapshot_FullRoundTrip_PreservesAllState()
        {
            var src = NewEngine();

            // Populate every manager
            var aliceId = src.AccountManager.Create();
            var bobId = src.AccountManager.Create();
            src.AccountManager.Account(aliceId)!.Balance.Deposit(5000);
            src.AccountManager.Account(bobId)!.Balance.Deposit(3000);

            var asset1 = new Asset(0, aliceId, 1, 100, 0, new byte[] { 0x10 }) { IsLockable = true };
            var asset2 = new Asset(0, bobId, 1, 200, 0, new byte[] { 0x20 });
            src.AssetManager.Create(asset1);
            src.AssetManager.Create(asset2);

            src.AssetBalanceManager.Deposit(1, 750); // treasury keyed by collection id
            src.LockManager.Lock(asset1.Id);
            src.MarketManager.List(asset2.Id, 999);

            // Advance the chain a few times
            src.BlockchainInfoProvider.IncrementBlockNumber();
            src.BlockchainInfoProvider.IncrementBlockNumber();

            var snapshot = src.Snapshot();

            // Restore into a brand new engine (different RNG seed to prove it gets overwritten)
            var dst = new Engine<ActionIdentifier, ActionRule>(
                new BlockchainInfoProvider(seed: 7),
                (p, r, a, b, c, m, s) => true);
            dst.Restore(snapshot);

            Assert.Multiple(() =>
            {
                // Accounts
                Assert.That(dst.AccountManager.Account(aliceId)!.Balance.Value, Is.EqualTo(5000));
                Assert.That(dst.AccountManager.Account(bobId)!.Balance.Value, Is.EqualTo(3000));

                // Assets
                var r1 = dst.AssetManager.Read(asset1.Id);
                var r2 = dst.AssetManager.Read(asset2.Id);
                Assert.That(r1, Is.Not.Null);
                Assert.That(r1!.OwnerId, Is.EqualTo(aliceId));
                Assert.That(r1.IsLockable, Is.True);
                Assert.That(r1.Data![0], Is.EqualTo(0x10));
                Assert.That(r2!.OwnerId, Is.EqualTo(bobId));

                // Treasury / balances
                Assert.That(dst.AssetBalanceManager.AssetBalance(1), Is.EqualTo(750));

                // Locks
                Assert.That(dst.LockManager.IsLocked(asset1.Id), Is.True);

                // Market
                Assert.That(dst.MarketManager.GetPrice(asset2.Id), Is.EqualTo(999));

                // Block number
                Assert.That(dst.BlockchainInfoProvider.CurrentBlockNumber,
                    Is.EqualTo(src.BlockchainInfoProvider.CurrentBlockNumber));
            });
        }

        [Test]
        public void Engine_Restore_VersionMismatch_Throws()
        {
            var dst = NewEngine();
            var bad = new byte[] { 99 }; // wrong envelope version
            Assert.Throws<InvalidDataException>(() => dst.Restore(bad));
        }

        [Test]
        public void Engine_Restore_RoundTripsAllAssetsViaAllAssets()
        {
            var src = NewEngine();
            var ownerId = src.AccountManager.Create();
            for (int i = 0; i < 5; i++)
            {
                src.AssetManager.Create(new Asset(0, ownerId, 1, (uint)i, 0, new byte[] { (byte)i }));
            }

            var snapshot = src.Snapshot();

            var dst = new Engine<ActionIdentifier, ActionRule>(
                new BlockchainInfoProvider(seed: 0),
                (p, r, a, b, c, m, s) => true);
            dst.Restore(snapshot);

            var all = dst.AssetManager.AllAssets().ToList();
            Assert.That(all, Has.Count.EqualTo(5));
        }
    }
}
