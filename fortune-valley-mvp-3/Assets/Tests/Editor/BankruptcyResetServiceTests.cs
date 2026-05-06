using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class BankruptcyResetServiceTests
    {
        private class FakeResettable : IBankruptcyResettable
        {
            public int ResetCallCount;
            public void OnBankruptcyReset() => ResetCallCount++;
        }

        private int _softResetFireCount;

        [SetUp]
        public void SetUp()
        {
            _softResetFireCount = 0;
            GameEvents.OnSoftBankruptcyReset += () => _softResetFireCount++;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void SingleRegistered_OnBankruptcy_FiresReset()
        {
            using (var svc = new BankruptcyResetService())
            {
                var fake = new FakeResettable();
                svc.Register(fake);

                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(1, fake.ResetCallCount);
            }
        }

        [Test]
        public void MultipleRegistered_AllReceiveReset()
        {
            using (var svc = new BankruptcyResetService())
            {
                var a = new FakeResettable();
                var b = new FakeResettable();
                var c = new FakeResettable();
                svc.Register(a);
                svc.Register(b);
                svc.Register(c);

                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(1, a.ResetCallCount);
                Assert.AreEqual(1, b.ResetCallCount);
                Assert.AreEqual(1, c.ResetCallCount);
            }
        }

        [Test]
        public void Register_IsIdempotent()
        {
            using (var svc = new BankruptcyResetService())
            {
                var fake = new FakeResettable();
                svc.Register(fake);
                svc.Register(fake); // duplicate

                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(1, fake.ResetCallCount,
                    "A resettable registered twice should still only fire once.");
            }
        }

        [Test]
        public void Unregister_RemovesFromList()
        {
            using (var svc = new BankruptcyResetService())
            {
                var fake = new FakeResettable();
                svc.Register(fake);
                svc.Unregister(fake);

                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(0, fake.ResetCallCount);
            }
        }

        [Test]
        public void BankruptcyFlag_StartsFalse_TrueAfterTrigger()
        {
            using (var svc = new BankruptcyResetService())
            {
                Assert.IsFalse(svc.BankruptcyFlag);

                GameEvents.RaiseBankruptcyTriggered();

                Assert.IsTrue(svc.BankruptcyFlag);
            }
        }

        [Test]
        public void OnSoftBankruptcyReset_FiresOnceAfterReset()
        {
            using (var svc = new BankruptcyResetService())
            {
                var fake = new FakeResettable();
                svc.Register(fake);

                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(1, _softResetFireCount);
            }
        }

        [Test]
        public void DoubleBankruptcy_BothInvokeAllResettables()
        {
            using (var svc = new BankruptcyResetService())
            {
                var fake = new FakeResettable();
                svc.Register(fake);

                GameEvents.RaiseBankruptcyTriggered();
                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(2, fake.ResetCallCount);
                Assert.AreEqual(2, _softResetFireCount);
                Assert.IsTrue(svc.BankruptcyFlag, "Flag stays true across multiple bankruptcies.");
            }
        }

        [Test]
        public void BatchLotResetAction_InvokedOnTrigger()
        {
            using (var svc = new BankruptcyResetService())
            {
                int actionCallCount = 0;
                svc.SetBatchLotResetAction(() => actionCallCount++);

                GameEvents.RaiseBankruptcyTriggered();

                Assert.AreEqual(1, actionCallCount);
            }
        }

        [Test]
        public void HydrateFlag_RestoresFromSave()
        {
            using (var svc = new BankruptcyResetService())
            {
                svc.HydrateFlag(true);

                Assert.IsTrue(svc.BankruptcyFlag);
            }
        }

        [Test]
        public void ResetForNewGame_ClearsFlag()
        {
            using (var svc = new BankruptcyResetService())
            {
                svc.HydrateFlag(true);
                svc.ResetForNewGame();

                Assert.IsFalse(svc.BankruptcyFlag);
            }
        }

        [Test]
        public void Dispose_UnsubscribesFromTrigger()
        {
            var svc = new BankruptcyResetService();
            var fake = new FakeResettable();
            svc.Register(fake);
            svc.Dispose();

            GameEvents.RaiseBankruptcyTriggered();

            Assert.AreEqual(0, fake.ResetCallCount);
        }
    }
}
