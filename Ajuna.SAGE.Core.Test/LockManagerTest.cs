using Ajuna.SAGE.Core.Manager;

namespace Ajuna.SAGE.Core.Test
{
    [TestFixture]
    public class LockManagerTest
    {
        private LockManager _lockManager;

        [SetUp]
        public void Setup()
        {
            _lockManager = new LockManager();
        }

        // --- Lock ---

        [Test]
        public void Lock_NewAsset_ReturnsTrue()
        {
            Assert.That(_lockManager.Lock(1), Is.True);
        }

        [Test]
        public void Lock_AlreadyLocked_ReturnsFalse()
        {
            _lockManager.Lock(1);
            Assert.That(_lockManager.Lock(1), Is.False);
        }

        [Test]
        public void Lock_AfterUnlock_ReturnsTrue()
        {
            _lockManager.Lock(1);
            _lockManager.Unlock(1);
            Assert.That(_lockManager.Lock(1), Is.True);
        }

        // --- Unlock ---

        [Test]
        public void Unlock_LockedAsset_ReturnsTrue()
        {
            _lockManager.Lock(1);
            Assert.That(_lockManager.Unlock(1), Is.True);
        }

        [Test]
        public void Unlock_NotLocked_ReturnsFalse()
        {
            Assert.That(_lockManager.Unlock(1), Is.False);
        }

        [Test]
        public void Unlock_AlreadyUnlocked_ReturnsFalse()
        {
            _lockManager.Lock(1);
            _lockManager.Unlock(1);
            Assert.That(_lockManager.Unlock(1), Is.False);
        }

        // --- CanLock ---

        [Test]
        public void CanLock_NewAsset_ReturnsTrue()
        {
            Assert.That(_lockManager.CanLock(1, out _), Is.True);
        }

        [Test]
        public void CanLock_LockedAsset_ReturnsFalse()
        {
            _lockManager.Lock(1);
            Assert.That(_lockManager.CanLock(1, out bool state), Is.False);
            Assert.That(state, Is.True); // currently locked
        }

        [Test]
        public void CanLock_UnlockedAsset_ReturnsTrue()
        {
            _lockManager.Lock(1);
            _lockManager.Unlock(1);
            Assert.That(_lockManager.CanLock(1, out bool state), Is.True);
            Assert.That(state, Is.False); // currently unlocked
        }

        // --- CanUnlock ---

        [Test]
        public void CanUnlock_LockedAsset_ReturnsTrue()
        {
            _lockManager.Lock(1);
            Assert.That(_lockManager.CanUnlock(1, out bool state), Is.True);
            Assert.That(state, Is.True);
        }

        [Test]
        public void CanUnlock_NotLocked_ReturnsFalse()
        {
            Assert.That(_lockManager.CanUnlock(1, out _), Is.False);
        }

        // --- IsLocked ---

        [Test]
        public void IsLocked_NewAsset_ReturnsNull()
        {
            Assert.That(_lockManager.IsLocked(1), Is.Null);
        }

        [Test]
        public void IsLocked_LockedAsset_ReturnsTrue()
        {
            _lockManager.Lock(1);
            Assert.That(_lockManager.IsLocked(1), Is.True);
        }

        [Test]
        public void IsLocked_UnlockedAsset_ReturnsFalse()
        {
            _lockManager.Lock(1);
            _lockManager.Unlock(1);
            Assert.That(_lockManager.IsLocked(1), Is.False);
        }

        // --- Multiple assets ---

        [Test]
        public void Lock_MultipleAssets_Independent()
        {
            _lockManager.Lock(1);
            _lockManager.Lock(2);

            Assert.That(_lockManager.IsLocked(1), Is.True);
            Assert.That(_lockManager.IsLocked(2), Is.True);

            _lockManager.Unlock(1);
            Assert.That(_lockManager.IsLocked(1), Is.False);
            Assert.That(_lockManager.IsLocked(2), Is.True);
        }
    }
}
