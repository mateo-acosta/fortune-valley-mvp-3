using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class InsolvencyMonitorTests
    {
        private float _checking;
        private float _investing;
        private float _ccDebt;
        private float _loanPrincipal;
        private int _bankruptcyFireCount;

        [SetUp]
        public void SetUp()
        {
            _checking = 0f;
            _investing = 0f;
            _ccDebt = 0f;
            _loanPrincipal = 0f;
            _bankruptcyFireCount = 0;
            GameEvents.OnBankruptcyTriggered += () => _bankruptcyFireCount++;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
        }

        private InsolvencyMonitor BuildMonitor()
        {
            return new InsolvencyMonitor(
                () => _checking,
                () => _investing,
                () => _ccDebt,
                () => _loanPrincipal);
        }

        [Test]
        public void SolventCycle_KeepsCounterAtZero()
        {
            _checking = 1000f;
            _ccDebt = 200f;

            using (var m = BuildMonitor())
            {
                m.EvaluateCycle();

                Assert.AreEqual(0, m.CurrentCounter);
                Assert.AreEqual(0, _bankruptcyFireCount);
            }
        }

        [Test]
        public void InsolventCycle_IncrementsCounter()
        {
            _checking = 500f;
            _ccDebt = 5000f;

            using (var m = BuildMonitor())
            {
                m.EvaluateCycle();

                Assert.AreEqual(1, m.CurrentCounter);
                Assert.AreEqual(0, _bankruptcyFireCount);
            }
        }

        [Test]
        public void RecoveryAfterFour_ResetsCounter()
        {
            _checking = 0f;
            _ccDebt = 5000f;

            using (var m = BuildMonitor())
            {
                m.EvaluateCycle(); // 1
                m.EvaluateCycle(); // 2
                m.EvaluateCycle(); // 3
                m.EvaluateCycle(); // 4
                Assert.AreEqual(4, m.CurrentCounter);

                _checking = 10000f; // become solvent
                m.EvaluateCycle();

                Assert.AreEqual(0, m.CurrentCounter);
                Assert.AreEqual(0, _bankruptcyFireCount);
            }
        }

        [Test]
        public void FiveConsecutiveInsolvent_FiresBankruptcy()
        {
            _checking = 0f;
            _ccDebt = 5000f;

            using (var m = BuildMonitor())
            {
                for (int i = 0; i < InsolvencyMonitor.InsolvencyThreshold; i++)
                {
                    m.EvaluateCycle();
                }

                Assert.AreEqual(1, _bankruptcyFireCount);
                Assert.AreEqual(0, m.CurrentCounter,
                    "Counter resets on bankruptcy fire so a recovered, then re-insolvent player cycles fresh.");
            }
        }

        [Test]
        public void Insolvency_UsesCheckingPlusInvesting_VsCCDebtPlusLoans()
        {
            // Liquid 600 < Debt 700 -> insolvent
            _checking = 100f;
            _investing = 500f;
            _ccDebt = 100f;
            _loanPrincipal = 600f;

            using (var m = BuildMonitor())
            {
                m.EvaluateCycle();
                Assert.AreEqual(1, m.CurrentCounter);
            }

            _bankruptcyFireCount = 0;

            // Now liquid 800 >= debt 700 -> solvent
            _checking = 200f;
            _investing = 600f;

            using (var m = BuildMonitor())
            {
                m.EvaluateCycle();
                Assert.AreEqual(0, m.CurrentCounter);
            }
        }

        [Test]
        public void OnMonthlyPaymentCycleComplete_DrivesEvaluation()
        {
            _checking = 0f;
            _ccDebt = 5000f;

            using (var m = BuildMonitor())
            {
                GameEvents.RaiseMonthlyPaymentCycleComplete();

                Assert.AreEqual(1, m.CurrentCounter);
            }
        }

        [Test]
        public void Dispose_UnsubscribesFromCycleEvent()
        {
            var m = BuildMonitor();
            m.Dispose();

            _checking = 0f;
            _ccDebt = 5000f;
            GameEvents.RaiseMonthlyPaymentCycleComplete();

            Assert.AreEqual(0, m.CurrentCounter);
        }
    }
}
