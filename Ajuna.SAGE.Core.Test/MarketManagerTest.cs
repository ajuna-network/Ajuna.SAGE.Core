using Ajuna.SAGE.Core.Manager;

namespace Ajuna.SAGE.Core.Test
{
    [TestFixture]
    public class MarketManagerTest
    {
        private MarketManager _marketManager;

        [SetUp]
        public void Setup()
        {
            _marketManager = new MarketManager();
        }

        // --- List ---

        [Test]
        public void List_NewAsset_ReturnsTrue()
        {
            Assert.That(_marketManager.List(1, 500), Is.True);
        }

        [Test]
        public void List_AlreadyListed_ReturnsFalse()
        {
            _marketManager.List(1, 500);
            Assert.That(_marketManager.List(1, 999), Is.False);
        }

        [Test]
        public void List_ZeroPrice_ReturnsFalse()
        {
            Assert.That(_marketManager.List(1, 0), Is.False);
        }

        [Test]
        public void List_AfterDelist_ReturnsTrue()
        {
            _marketManager.List(1, 500);
            _marketManager.Delist(1);
            Assert.That(_marketManager.List(1, 999), Is.True);
        }

        // --- Delist ---

        [Test]
        public void Delist_ListedAsset_ReturnsTrue()
        {
            _marketManager.List(1, 500);
            Assert.That(_marketManager.Delist(1), Is.True);
        }

        [Test]
        public void Delist_NotListed_ReturnsFalse()
        {
            Assert.That(_marketManager.Delist(1), Is.False);
        }

        [Test]
        public void Delist_AlreadyDelisted_ReturnsFalse()
        {
            _marketManager.List(1, 500);
            _marketManager.Delist(1);
            Assert.That(_marketManager.Delist(1), Is.False);
        }

        // --- GetPrice ---

        [Test]
        public void GetPrice_ListedAsset_ReturnsPrice()
        {
            _marketManager.List(1, 500);
            Assert.That(_marketManager.GetPrice(1), Is.EqualTo(500));
        }

        [Test]
        public void GetPrice_NotListed_ReturnsNull()
        {
            Assert.That(_marketManager.GetPrice(1), Is.Null);
        }

        [Test]
        public void GetPrice_AfterDelist_ReturnsNull()
        {
            _marketManager.List(1, 500);
            _marketManager.Delist(1);
            Assert.That(_marketManager.GetPrice(1), Is.Null);
        }

        [Test]
        public void GetPrice_RelistWithNewPrice_ReturnsNewPrice()
        {
            _marketManager.List(1, 500);
            _marketManager.Delist(1);
            _marketManager.List(1, 999);
            Assert.That(_marketManager.GetPrice(1), Is.EqualTo(999));
        }

        // --- IsListed ---

        [Test]
        public void IsListed_ListedAsset_ReturnsTrue()
        {
            _marketManager.List(1, 500);
            Assert.That(_marketManager.IsListed(1), Is.True);
        }

        [Test]
        public void IsListed_NotListed_ReturnsFalse()
        {
            Assert.That(_marketManager.IsListed(1), Is.False);
        }

        // --- GetAllListings ---

        [Test]
        public void GetAllListings_Empty_ReturnsEmpty()
        {
            Assert.That(_marketManager.GetAllListings().Count(), Is.EqualTo(0));
        }

        [Test]
        public void GetAllListings_MultipleListings_ReturnsAll()
        {
            _marketManager.List(1, 100);
            _marketManager.List(2, 200);
            _marketManager.List(3, 300);

            var listings = _marketManager.GetAllListings().ToList();
            Assert.That(listings.Count, Is.EqualTo(3));
            Assert.That(listings.Any(l => l.assetId == 1 && l.price == 100), Is.True);
            Assert.That(listings.Any(l => l.assetId == 2 && l.price == 200), Is.True);
            Assert.That(listings.Any(l => l.assetId == 3 && l.price == 300), Is.True);
        }

        [Test]
        public void GetAllListings_AfterDelist_ExcludesDelisted()
        {
            _marketManager.List(1, 100);
            _marketManager.List(2, 200);
            _marketManager.Delist(1);

            var listings = _marketManager.GetAllListings().ToList();
            Assert.That(listings.Count, Is.EqualTo(1));
            Assert.That(listings[0].assetId, Is.EqualTo(2));
        }

        // --- Multiple assets ---

        [Test]
        public void MultipleAssets_Independent()
        {
            _marketManager.List(1, 100);
            _marketManager.List(2, 200);

            Assert.That(_marketManager.GetPrice(1), Is.EqualTo(100));
            Assert.That(_marketManager.GetPrice(2), Is.EqualTo(200));

            _marketManager.Delist(1);
            Assert.That(_marketManager.IsListed(1), Is.False);
            Assert.That(_marketManager.IsListed(2), Is.True);
        }
    }
}
