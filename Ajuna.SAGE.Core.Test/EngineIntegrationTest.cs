using Ajuna.SAGE.Core.Model;
using Moq;

namespace Ajuna.SAGE.Core.Test
{
    [TestFixture]
    public class EngineIntegrationTest
    {
        private Mock<IBlockchainInfoProvider> _mockBlockchainInfo;
        private Engine<ActionIdentifier, ActionRule> _engine;

        [SetUp]
        public void Setup()
        {
            _mockBlockchainInfo = new Mock<IBlockchainInfoProvider>();
            _mockBlockchainInfo.Setup(m => m.GenerateRandomHash()).Returns(new byte[] { 1, 2, 3, 4 });
            _mockBlockchainInfo.Setup(m => m.CurrentBlockNumber).Returns(100);

            _engine = new Engine<ActionIdentifier, ActionRule>(
                _mockBlockchainInfo.Object,
                (p, r, a, b, c, m, s) => true);
        }

        // --- Lock check integration ---

        [Test]
        public void Transition_LockableButNotLocked_Succeeds()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            var rules = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 1);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) => w;

            _engine.AddTransition(identifier, new[] { rules }, default, function);

            // Asset is lockable but NOT locked — should pass
            var asset = new Asset(0, player.Id, 1, 50, 0, new byte[8]);
            asset.IsLockable = true;
            _engine.AssetManager.Create(asset);

            bool success = _engine.Transition(player, identifier, new IAsset[] { asset }, out IAsset[] result);

            Assert.That(success, Is.True);
        }

        [Test]
        public void Transition_LockableAndLocked_Throws()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            var rules = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 1);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) => w;

            _engine.AddTransition(identifier, new[] { rules }, default, function);

            // Asset is lockable AND locked — should throw
            var asset = new Asset(0, player.Id, 1, 50, 0, new byte[8]);
            asset.IsLockable = true;
            _engine.AssetManager.Create(asset);
            _engine.LockManager.Lock(asset.Id);

            Assert.Throws<NotSupportedException>(() =>
                _engine.Transition(player, identifier, new IAsset[] { asset }, out _));
        }

        [Test]
        public void Transition_NotLockable_AlwaysSucceeds()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            var rules = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 1);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) => w;

            _engine.AddTransition(identifier, new[] { rules }, default, function);

            // Asset not lockable — should always pass even if somehow in lock manager
            var asset = new Asset(0, player.Id, 1, 50, 0, new byte[8]);
            asset.IsLockable = false;
            _engine.AssetManager.Create(asset);

            bool success = _engine.Transition(player, identifier, new IAsset[] { asset }, out _);
            Assert.That(success, Is.True);
        }

        [Test]
        public void Transition_LockAndUnlockViaLockManager_ControlsAccess()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            var rules = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 1);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) => w;

            _engine.AddTransition(identifier, new[] { rules }, default, function);

            var asset = new Asset(0, player.Id, 1, 50, 0, new byte[8]);
            asset.IsLockable = true;
            _engine.AssetManager.Create(asset);

            // Lock it — transition should fail
            _engine.LockManager.Lock(asset.Id);
            Assert.Throws<NotSupportedException>(() =>
                _engine.Transition(player, identifier, new IAsset[] { asset }, out _));

            // Unlock it — transition should succeed again
            _engine.LockManager.Unlock(asset.Id);
            bool success = _engine.Transition(player, identifier, new IAsset[] { asset }, out _);
            Assert.That(success, Is.True);
        }

        // --- Reconciliation ---

        [Test]
        public void Transition_Reconciliation_CreatesNewAssets()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
            {
                return new IAsset[] { new Asset(Utils.GenerateRandomId(), player.Id, 1, 42, 0, new byte[8]) };
            };

            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), default, function);

            _engine.Transition(player, identifier, null, out IAsset[] result);

            Assert.That(result.Length, Is.EqualTo(1));
            // Asset should be stored in manager
            var stored = _engine.AssetManager.Read(result[0].Id);
            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.Score, Is.EqualTo(42));
        }

        [Test]
        public void Transition_Reconciliation_DeletesConsumedAssets()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var asset = new Asset(0, player.Id, 1, 50, 0, new byte[8]);
            _engine.AssetManager.Create(asset);

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            // Return empty — input asset consumed
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
                Enumerable.Empty<IAsset>();

            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), default, function);

            _engine.Transition(player, identifier, new IAsset[] { asset }, out _);

            Assert.That(_engine.AssetManager.Read(asset.Id), Is.Null);
        }

        [Test]
        public void Transition_Reconciliation_UpdatesModifiedAssets()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var asset = new Asset(0, player.Id, 1, 10, 0, new byte[8]);
            _engine.AssetManager.Create(asset);
            var originalId = asset.Id;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            // Modify score and return same asset — should UPDATE
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
            {
                var a = w.First();
                a.Score = 99;
                return new IAsset[] { a };
            };

            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), default, function);

            _engine.Transition(player, identifier, new IAsset[] { asset }, out IAsset[] result);

            Assert.Multiple(() =>
            {
                Assert.That(result[0].Score, Is.EqualTo(99));
                // Stored asset should have the updated score
                var stored = _engine.AssetManager.Read(originalId);
                Assert.That(stored, Is.Not.Null);
                Assert.That(stored!.Score, Is.EqualTo(99));
            });
        }

        [Test]
        public void Transition_Reconciliation_UpdateFromOutputs_NotInputs()
        {
            // This tests the bug fix: updates should source from outputs, not inputs
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            var asset = new Asset(0, player.Id, 1, 10, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
            _engine.AssetManager.Create(asset);
            var originalId = asset.Id;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            // Create a NEW object with the same ID but different data
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
            {
                var original = w.First();
                var modified = new Asset(original.Id, original.OwnerId, original.CollectionId,
                    77, original.Genesis, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });
                return new IAsset[] { modified };
            };

            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), default, function);

            _engine.Transition(player, identifier, new IAsset[] { asset }, out IAsset[] result);

            Assert.Multiple(() =>
            {
                // Output should have the modified values
                Assert.That(result[0].Score, Is.EqualTo(77));
                Assert.That(result[0].Data![0], Is.EqualTo(0xFF));

                // Engine storage should match output, NOT input
                var stored = _engine.AssetManager.Read(originalId);
                Assert.That(stored!.Score, Is.EqualTo(77));
                Assert.That(stored.Data![0], Is.EqualTo(0xFF));
            });
        }

        // --- Market Manager accessible ---

        [Test]
        public void MarketManager_AccessibleFromEngine()
        {
            Assert.That(_engine.MarketManager, Is.Not.Null);

            _engine.MarketManager.List(1, 500);
            Assert.That(_engine.MarketManager.IsListed(1), Is.True);
            Assert.That(_engine.MarketManager.GetPrice(1), Is.EqualTo(500));
        }

        [Test]
        public void Transition_MarketManagerPassedToFunction()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;

            bool marketManagerReceived = false;

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
            {
                // k is the market manager
                marketManagerReceived = k != null;
                k?.List(42, 999);
                return Enumerable.Empty<IAsset>();
            };

            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), default, function);

            _engine.Transition(player, identifier, null, out _);

            Assert.That(marketManagerReceived, Is.True);
            Assert.That(_engine.MarketManager.GetPrice(42), Is.EqualTo(999));
        }

        // --- Fee deduction ---

        [Test]
        public void Transition_WithFee_DeductsFromBalance()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;
            player.Balance.Deposit(1000);

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
                Enumerable.Empty<IAsset>();

            var fee = new TransitionFee(100);
            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), fee, function);

            _engine.Transition(player, identifier, null, out _);

            Assert.That(player.Balance.Value, Is.EqualTo(900));
        }

        [Test]
        public void Transition_InsufficientBalanceForFee_ReturnsFalse()
        {
            var playerId = _engine.AccountManager.Create();
            var player = _engine.AccountManager.Account(playerId)!;
            player.Balance.Deposit(50);

            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m, l, k) =>
                Enumerable.Empty<IAsset>();

            var fee = new TransitionFee(100);
            _engine.AddTransition(identifier, Array.Empty<ActionRule>(), fee, function);

            bool success = _engine.Transition(player, identifier, null, out IAsset[] result);

            Assert.That(success, Is.False);
            Assert.That(result, Is.Empty);
            Assert.That(player.Balance.Value, Is.EqualTo(50)); // unchanged
        }
    }
}
