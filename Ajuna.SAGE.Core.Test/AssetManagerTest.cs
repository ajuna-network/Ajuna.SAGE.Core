using Ajuna.SAGE.Core.Manager;
using Ajuna.SAGE.Core.Model;

namespace Ajuna.SAGE.Core.Test
{
    [TestFixture]
    public class AssetManagerTest
    {
        private AssetManager _assetManager;
        private AccountManager _accountManager;

        [SetUp]
        public void Setup()
        {
            _assetManager = new AssetManager();
            _accountManager = new AccountManager();
        }

        // --- Create ---

        [Test]
        public void Create_AssignsId()
        {
            var asset = new Asset(0, 1, 1, 0, 0, new byte[8]);
            var id = _assetManager.Create(asset);

            Assert.That(id, Is.GreaterThan(0));
            Assert.That(asset.Id, Is.EqualTo(id));
        }

        [Test]
        public void Create_SequentialIds()
        {
            var a1 = new Asset(0, 1, 1, 0, 0, new byte[8]);
            var a2 = new Asset(0, 1, 1, 0, 0, new byte[8]);

            var id1 = _assetManager.Create(a1);
            var id2 = _assetManager.Create(a2);

            Assert.That(id2, Is.EqualTo(id1 + 1));
        }

        // --- Read ---

        [Test]
        public void Read_ExistingAsset_ReturnsAsset()
        {
            var asset = new Asset(0, 1, 1, 42, 0, new byte[8]);
            var id = _assetManager.Create(asset);

            var read = _assetManager.Read(id);
            Assert.That(read, Is.Not.Null);
            Assert.That(read!.Score, Is.EqualTo(42));
        }

        [Test]
        public void Read_NonExistent_ReturnsNull()
        {
            Assert.That(_assetManager.Read(99999), Is.Null);
        }

        // --- Update ---

        [Test]
        public void Update_ExistingAsset_ReturnsTrue()
        {
            var asset = new Asset(0, 1, 1, 10, 0, new byte[8]);
            var id = _assetManager.Create(asset);

            asset.Score = 99;
            Assert.That(_assetManager.Update(asset), Is.True);

            var read = _assetManager.Read(id);
            Assert.That(read!.Score, Is.EqualTo(99));
        }

        [Test]
        public void Update_NonExistent_ReturnsFalse()
        {
            var asset = new Asset(99999, 1, 1, 0, 0, new byte[8]);
            Assert.That(_assetManager.Update(asset), Is.False);
        }

        // --- Delete ---

        [Test]
        public void Delete_ExistingAsset_ReturnsTrue()
        {
            var asset = new Asset(0, 1, 1, 0, 0, new byte[8]);
            var id = _assetManager.Create(asset);

            Assert.That(_assetManager.Delete(id), Is.True);
            Assert.That(_assetManager.Read(id), Is.Null);
        }

        [Test]
        public void Delete_NonExistent_ReturnsFalse()
        {
            Assert.That(_assetManager.Delete(99999), Is.False);
        }

        [Test]
        public void Delete_ByAsset_Works()
        {
            var asset = new Asset(0, 1, 1, 0, 0, new byte[8]);
            _assetManager.Create(asset);

            Assert.That(_assetManager.Delete(asset), Is.True);
            Assert.That(_assetManager.Read(asset.Id), Is.Null);
        }

        // --- AssetOf ---

        [Test]
        public void AssetOf_ReturnsOwnedAssets()
        {
            var playerId = _accountManager.Create();
            var player = _accountManager.Account(playerId)!;

            var a1 = new Asset(0, player.Id, 1, 10, 0, new byte[8]);
            var a2 = new Asset(0, player.Id, 1, 20, 0, new byte[8]);
            var a3 = new Asset(0, 999, 1, 30, 0, new byte[8]); // different owner
            _assetManager.Create(a1);
            _assetManager.Create(a2);
            _assetManager.Create(a3);

            var owned = _assetManager.AssetOf(player).ToList();
            Assert.That(owned.Count, Is.EqualTo(2));
            Assert.That(owned.All(a => a.OwnerId == player.Id), Is.True);
        }

        [Test]
        public void AssetOf_NoAssets_ReturnsEmpty()
        {
            var playerId = _accountManager.Create();
            var player = _accountManager.Account(playerId)!;

            var owned = _assetManager.AssetOf(player).ToList();
            Assert.That(owned.Count, Is.EqualTo(0));
        }

        [Test]
        public void AssetOf_AfterDelete_ExcludesDeleted()
        {
            var playerId = _accountManager.Create();
            var player = _accountManager.Account(playerId)!;

            var a1 = new Asset(0, player.Id, 1, 10, 0, new byte[8]);
            var a2 = new Asset(0, player.Id, 1, 20, 0, new byte[8]);
            _assetManager.Create(a1);
            _assetManager.Create(a2);

            _assetManager.Delete(a1);
            var owned = _assetManager.AssetOf(player).ToList();
            Assert.That(owned.Count, Is.EqualTo(1));
            Assert.That(owned[0].Score, Is.EqualTo(20));
        }
    }
}
