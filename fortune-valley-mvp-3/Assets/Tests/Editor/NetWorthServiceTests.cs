using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class NetWorthServiceTests
    {
        private float _liquid;
        private float _business;
        private float _lastTotal;
        private float _lastLiquid;
        private int _eventFireCount;

        [SetUp]
        public void SetUp()
        {
            _liquid = 0f;
            _business = 0f;
            _lastTotal = 0f;
            _lastLiquid = 0f;
            _eventFireCount = 0;
            GameEvents.OnNetWorthChanged += CaptureEvent;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
        }

        private void CaptureEvent(float total, float liquid)
        {
            _lastTotal = total;
            _lastLiquid = liquid;
            _eventFireCount++;
        }

        private NetWorthService BuildService(bool withBusinessFunc = true)
        {
            return withBusinessFunc
                ? new NetWorthService(() => _liquid, () => _business)
                : new NetWorthService(() => _liquid);
        }

        [Test]
        public void EmptyState_ZeroNetWorth()
        {
            using (var svc = BuildService())
            {
                Assert.AreEqual(0f, svc.LiquidNetWorth);
                Assert.AreEqual(0f, svc.TotalNetWorth);
            }
        }

        [Test]
        public void LiquidOnly_ReturnsLiquid()
        {
            _liquid = 1500f;

            using (var svc = BuildService())
            {
                Assert.AreEqual(1500f, svc.LiquidNetWorth);
                Assert.AreEqual(1500f, svc.TotalNetWorth);
            }
        }

        [Test]
        public void LiquidPlusBusiness_TotalIncludesBoth()
        {
            _liquid = 5000f;
            _business = 250000f;

            using (var svc = BuildService())
            {
                Assert.AreEqual(5000f, svc.LiquidNetWorth);
                Assert.AreEqual(255000f, svc.TotalNetWorth);
            }
        }

        [Test]
        public void NegativeLiquid_FromDebt_PropagatesToTotal()
        {
            _liquid = -3000f; // debts exceed assets
            _business = 50000f;

            using (var svc = BuildService())
            {
                Assert.AreEqual(-3000f, svc.LiquidNetWorth);
                Assert.AreEqual(47000f, svc.TotalNetWorth);
            }
        }

        [Test]
        public void Pump_FiresEventOnFirstCall()
        {
            _liquid = 1000f;
            _business = 500f;

            using (var svc = BuildService())
            {
                svc.Pump();

                Assert.AreEqual(1, _eventFireCount);
                Assert.AreEqual(1500f, _lastTotal);
                Assert.AreEqual(1000f, _lastLiquid);
            }
        }

        [Test]
        public void Pump_DoesNotFireWhenClean()
        {
            using (var svc = BuildService())
            {
                svc.Pump(); // initial fire
                _eventFireCount = 0;

                svc.Pump(); // no dirty flag, should not fire

                Assert.AreEqual(0, _eventFireCount);
            }
        }

        [Test]
        public void MarkDirty_TriggersFireOnNextPump()
        {
            using (var svc = BuildService())
            {
                svc.Pump();
                _eventFireCount = 0;

                _liquid = 9000f;
                svc.MarkDirty();
                svc.Pump();

                Assert.AreEqual(1, _eventFireCount);
                Assert.AreEqual(9000f, _lastTotal);
            }
        }

        [Test]
        public void Pump_DoesNotFireForChangeBelowEpsilon()
        {
            _liquid = 1000f;

            using (var svc = BuildService())
            {
                svc.Pump();
                _eventFireCount = 0;

                _liquid = 1000.001f; // change is within ChangeEpsilon (0.01)
                svc.MarkDirty();
                svc.Pump();

                Assert.AreEqual(0, _eventFireCount);
            }
        }

        [Test]
        public void OnTick_DrivesPump()
        {
            _liquid = 1000f;

            using (var svc = BuildService())
            {
                GameEvents.RaiseTick(1);

                Assert.AreEqual(1, _eventFireCount);
                Assert.AreEqual(1000f, _lastTotal);
            }
        }

        [Test]
        public void OnCheckingBalanceChanged_MarksDirty()
        {
            using (var svc = BuildService())
            {
                svc.Pump(); // initial
                _eventFireCount = 0;

                _liquid = 5000f;
                GameEvents.RaiseCheckingBalanceChanged(5000f, 5000f);
                svc.Pump();

                Assert.AreEqual(1, _eventFireCount);
                Assert.AreEqual(5000f, _lastLiquid);
            }
        }

        [Test]
        public void NoBusinessFunc_TotalEqualsLiquid()
        {
            _liquid = 7500f;
            _business = 999999f; // ignored because func not passed

            using (var svc = BuildService(withBusinessFunc: false))
            {
                Assert.AreEqual(7500f, svc.LiquidNetWorth);
                Assert.AreEqual(7500f, svc.TotalNetWorth);
            }
        }

        [Test]
        public void Constructor_RejectsNullLiquidFunc()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new NetWorthService(null));
        }

        [Test]
        public void Dispose_UnsubscribesFromEvents()
        {
            var svc = BuildService();
            svc.Pump();
            _eventFireCount = 0;

            svc.Dispose();

            // Tick after dispose should not fire OnNetWorthChanged
            GameEvents.RaiseTick(99);

            Assert.AreEqual(0, _eventFireCount);
        }
    }
}
