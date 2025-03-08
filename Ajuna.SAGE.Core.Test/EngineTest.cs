using Ajuna.SAGE.Core.Model;
using Moq;

namespace Ajuna.SAGE.Core.Test
{
    [TestFixture]
    public class EngineTest
    {
        private Mock<IBlockchainInfoProvider> _mockBlockchainInfoProvider;
        private Engine<ActionIdentifier, ActionRule> _engine;

        [SetUp]
        public void Setup()
        {
            // Create a mock without passing any constructor arguments
            _mockBlockchainInfoProvider = new Mock<IBlockchainInfoProvider>();

            // Set up the necessary methods in the mock
            _mockBlockchainInfoProvider.Setup(m => m.GenerateRandomHash()).Returns([1, 2, 3, 4]);
            _mockBlockchainInfoProvider.Setup(m => m.CurrentBlockNumber).Returns(100);

            _engine = new Engine<ActionIdentifier, ActionRule>(_mockBlockchainInfoProvider.Object, (p, r, a, b, c, m, s) => true);
        }

        [Test]
        public void Test_AddTransition_And_Transition_Valid()
        {
            string playerIdHex = "0xb4e21f9a";
            string assetIdHex = "0x3e4a6f8d";
            byte collectionId = 1;
            uint score = 50;
            uint genesis = 0;

            var playerId = BitConverter.ToUInt32(Utils.HexToBytes(playerIdHex));
            var player = new Account(playerId);

            // Arrange
            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);

            var rules = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 1);

            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m) =>
            {
                var asset = w.First();
                asset.Score += 10;
                return new List<IAsset> { asset };
            };

            _engine.AddTransition(identifier, [rules], default, function);

            var assetId = BitConverter.ToUInt32(Utils.HexToBytes(assetIdHex));
            var assets = new IAsset[]
            {
                new Asset(assetId, 0, collectionId, score, genesis, [])
            };

            // Act
            var flag = _engine.Transition(player, identifier, assets, out IAsset[] result);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(flag, Is.True, "The transition should be true.");
                Assert.That(result.Length, Is.EqualTo(1), "There should be one resulting asset.");
                Assert.That(result.First().Score, Is.EqualTo(60), "The resulting asset's score should be 60.");
            });
        }

        [Test]
        public void Test_Transition_DuplicateAssets_ThrowsException()
        {
            string playerIdHex = "0xb4e21f9a";
            string assetIdHex = "0x3e4a6f8d";
            byte collectionId = 1;
            uint score = 50;
            uint genesis = 0;

            var playerId = BitConverter.ToUInt32(Utils.HexToBytes(playerIdHex));
            var player = new Account(playerId);

            // Arrange
            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            var rules = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 1);

            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m) => w.Select(a => a);

            _engine.AddTransition(identifier, [rules], default, function);

            var assetId = BitConverter.ToUInt32(Utils.HexToBytes(assetIdHex));
            var duplicateAsset = new Asset(assetId, 0, collectionId, score, genesis, []);
            var assets = new Asset[] { duplicateAsset, duplicateAsset };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _engine.Transition(player, identifier, assets, out IAsset[] result), "Trying to Forge duplicates.");
        }

        [Test]
        public void Test_Transition_UnsupportedIdentifier_ThrowsException()
        {
            string playerIdHex = "0xb4e21f9a";
            string assetIdHex = "0x3e4a6f8d";
            byte collectionId = 1;
            uint score = 50;
            uint genesis = 0;

            var playerId = BitConverter.ToUInt32(Utils.HexToBytes(playerIdHex));
            var player = new Account(playerId);

            // Arrange
            var unsupportedIdentifier = new ActionIdentifier((ActionType)99, (ActionSubType)99);
            var assetId = BitConverter.ToUInt32(Utils.HexToBytes(assetIdHex));
            var assets = new Asset[]
            {
                new Asset(assetId, 0, collectionId, score, genesis,[])
            };

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => _engine.Transition(player, unsupportedIdentifier, assets, out IAsset[] result), "Unsupported Transition for Identifier.");
        }

        [Test]
        public void Test_Transition_InvalidAssetCount_ReturnsFalse()
        {
            string playerIdHex = "0xb4e21f9a";
            string assetIdHex = "0x3e4a6f8d";
            byte collectionId = 1;
            uint score = 50;
            uint genesis = 0;

            var playerId = BitConverter.ToUInt32(Utils.HexToBytes(playerIdHex));
            var player = new Account(playerId);

            // Arrange
            var identifier = new ActionIdentifier(ActionType.TypeA, ActionSubType.TypeX);
            var rule = new ActionRule(ActionRuleType.MinAsset, ActionRuleOp.GreaterEqual, 2);

            TransitionFunction<ActionRule> function = (e, r, f, w, h, b, c, m) => w.Select(a => a);

            var blockchainInfoProvider = new Mock<IBlockchainInfoProvider>();
            blockchainInfoProvider.Setup(b => b.GenerateRandomHash()).Returns(new byte[] { 0x00 });
            blockchainInfoProvider.Setup(b => b.CurrentBlockNumber).Returns(1u);

            // Setting up the Engine with custom Verify function
            var engine = new EngineBuilder<ActionIdentifier, ActionRule>(blockchainInfoProvider.Object)
                .SetVerifyFunction((p, r, a, b, c, m, s) =>
                {
                    if (r.RuleType == (byte)ActionRuleType.MinAsset && r.RuleOp == (byte)ActionRuleOp.GreaterEqual)
                    {
                        return a.Length >= BitConverter.ToUInt32(r.RuleValue);
                    }

                    return false;
                })
                .AddTransition(identifier, [rule], default, function)
                .Build();

            var assetId = BitConverter.ToUInt32(Utils.HexToBytes(assetIdHex));
            var assets = new Asset[]
            {
                new(assetId, 0, collectionId, score, genesis, [])
            };

            // Act
            bool success = engine.Transition(player, identifier, assets, out IAsset[] result);

            // Assert
            Assert.That(success, Is.False, "Expected Transition to return false due to insufficient assets.");
            Assert.That(result, Is.Empty, "Expected result to be empty when transition fails.");
        }

    }
}