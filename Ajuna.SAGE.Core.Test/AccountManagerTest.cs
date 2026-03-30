using Ajuna.SAGE.Core.Manager;

namespace Ajuna.SAGE.Core.Test
{
    [TestFixture]
    public class AccountManagerTest
    {
        private AccountManager _accountManager;

        [SetUp]
        public void Setup()
        {
            _accountManager = new AccountManager();
        }

        [Test]
        public void EngineId_CreatedAutomatically()
        {
            Assert.That(_accountManager.EngineId, Is.GreaterThan(0));
            var engineAccount = _accountManager.Account(_accountManager.EngineId);
            Assert.That(engineAccount, Is.Not.Null);
        }

        [Test]
        public void Create_ReturnsUniqueIds()
        {
            var id1 = _accountManager.Create();
            var id2 = _accountManager.Create();
            var id3 = _accountManager.Create();

            Assert.That(id1, Is.Not.EqualTo(id2));
            Assert.That(id2, Is.Not.EqualTo(id3));
        }

        [Test]
        public void Account_ExistingId_ReturnsAccount()
        {
            var id = _accountManager.Create();
            var account = _accountManager.Account(id);

            Assert.That(account, Is.Not.Null);
            Assert.That(account!.Id, Is.EqualTo(id));
        }

        [Test]
        public void Account_NonExistentId_ReturnsNull()
        {
            Assert.That(_accountManager.Account(99999999), Is.Null);
        }

        [Test]
        public void Account_InitialBalanceIsZero()
        {
            var id = _accountManager.Create();
            var account = _accountManager.Account(id)!;

            Assert.That(account.Balance.Value, Is.EqualTo(0));
        }

        [Test]
        public void Destroy_ExistingAccount_ReturnsTrue()
        {
            var id = _accountManager.Create();
            Assert.That(_accountManager.Destroy(id), Is.True);
        }

        [Test]
        public void Destroy_NonExistent_ReturnsFalse()
        {
            Assert.That(_accountManager.Destroy(99999999), Is.False);
        }

        [Test]
        public void Destroy_AccountNoLongerAccessible()
        {
            var id = _accountManager.Create();
            _accountManager.Destroy(id);

            Assert.That(_accountManager.Account(id), Is.Null);
        }

        [Test]
        public void Account_BalanceCanBeModified()
        {
            var id = _accountManager.Create();
            var account = _accountManager.Account(id)!;

            account.Balance.Deposit(1000);
            Assert.That(account.Balance.Value, Is.EqualTo(1000));

            account.Balance.Withdraw(300);
            Assert.That(account.Balance.Value, Is.EqualTo(700));
        }
    }
}
