using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class TransactionHistoryTests
    {
        private TransactionHistory _history;

        [SetUp]
        public void SetUp()
        {
            _history = new TransactionHistory(5);
        }

        [Test]
        public void NewHistory_HasZeroCount()
        {
            Assert.AreEqual(0, _history.Count);
        }

        [Test]
        public void Record_IncreasesCount()
        {
            _history.Record(TransactionType.LoanPayment, "test", 100f, 1);
            Assert.AreEqual(1, _history.Count);
        }

        [Test]
        public void GetAll_ReturnsNewestFirst()
        {
            _history.Record(TransactionType.LoanPayment, "first", 10f, 1);
            _history.Record(TransactionType.LoanPayment, "second", 20f, 2);
            _history.Record(TransactionType.LoanPayment, "third", 30f, 3);

            var all = _history.GetAll();

            Assert.AreEqual(3, all.Count);
            Assert.AreEqual("third", all[0].Description);
            Assert.AreEqual("second", all[1].Description);
            Assert.AreEqual("first", all[2].Description);
        }

        [Test]
        public void CircularBuffer_EvictsOldestAtCapacity()
        {
            // Capacity is 5, add 7 entries
            for (int i = 0; i < 7; i++)
                _history.Record(TransactionType.LoanPayment, $"entry{i}", i * 10f, i);

            Assert.AreEqual(5, _history.Count);

            var all = _history.GetAll();
            // Should have entries 2-6 (0 and 1 evicted), newest first
            Assert.AreEqual("entry6", all[0].Description);
            Assert.AreEqual("entry5", all[1].Description);
            Assert.AreEqual("entry4", all[2].Description);
            Assert.AreEqual("entry3", all[3].Description);
            Assert.AreEqual("entry2", all[4].Description);
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            _history.Record(TransactionType.LoanPayment, "loan1", 100f, 1);
            _history.Record(TransactionType.CreditCardCharge, "cc1", 50f, 2);
            _history.Record(TransactionType.LoanPayment, "loan2", 200f, 3);
            _history.Record(TransactionType.InsurancePurchased, "ins1", 40f, 4);

            var loanRecords = _history.GetByType(TransactionType.LoanPayment);

            Assert.AreEqual(2, loanRecords.Count);
            Assert.AreEqual("loan2", loanRecords[0].Description);
            Assert.AreEqual("loan1", loanRecords[1].Description);
        }

        [Test]
        public void GetByTypes_ReturnsMultipleMatchingTypes()
        {
            _history.Record(TransactionType.LoanPayment, "loan", 100f, 1);
            _history.Record(TransactionType.CreditCardCharge, "cc", 50f, 2);
            _history.Record(TransactionType.InsurancePurchased, "ins", 40f, 3);
            _history.Record(TransactionType.LoanPaidOff, "paid", 0f, 4);

            var creditRelated = _history.GetByTypes(
                TransactionType.LoanPayment,
                TransactionType.LoanPaidOff,
                TransactionType.CreditCardCharge);

            Assert.AreEqual(3, creditRelated.Count);
            Assert.AreEqual("paid", creditRelated[0].Description);
            Assert.AreEqual("cc", creditRelated[1].Description);
            Assert.AreEqual("loan", creditRelated[2].Description);
        }

        [Test]
        public void GetAll_EmptyHistory_ReturnsEmptyList()
        {
            var all = _history.GetAll();
            Assert.AreEqual(0, all.Count);
        }

        [Test]
        public void GetByType_NoMatches_ReturnsEmptyList()
        {
            _history.Record(TransactionType.LoanPayment, "loan", 100f, 1);

            var results = _history.GetByType(TransactionType.CreditCardCharge);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Clear_ResetsHistory()
        {
            _history.Record(TransactionType.LoanPayment, "test", 100f, 1);
            _history.Record(TransactionType.LoanPayment, "test2", 200f, 2);

            _history.Clear();

            Assert.AreEqual(0, _history.Count);
            Assert.AreEqual(0, _history.GetAll().Count);
        }

        [Test]
        public void Record_PreservesAllFields()
        {
            _history.Record(TransactionType.AccidentResolved, "fire damage", 500f, 42);

            var all = _history.GetAll();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual(TransactionType.AccidentResolved, all[0].Type);
            Assert.AreEqual("fire damage", all[0].Description);
            Assert.AreEqual(500f, all[0].Amount, 0.01f);
            Assert.AreEqual(42, all[0].Tick);
        }

        [Test]
        public void Record_WithEntityId_PreservesEntityId()
        {
            _history.Record(TransactionType.PremiumCharged, "premium", 50f, 1, "lot_1");

            var all = _history.GetAll();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("lot_1", all[0].EntityId);
        }

        [Test]
        public void Record_WithoutEntityId_EntityIdIsNull()
        {
            _history.Record(TransactionType.LoanPayment, "loan", 100f, 1);

            var all = _history.GetAll();
            Assert.AreEqual(1, all.Count);
            Assert.IsNull(all[0].EntityId);
        }

        [Test]
        public void CircularBuffer_WorksAfterMultipleWraps()
        {
            // Capacity 5, add 13 entries (wraps around twice+)
            for (int i = 0; i < 13; i++)
                _history.Record(TransactionType.LoanPayment, $"e{i}", i, i);

            Assert.AreEqual(5, _history.Count);

            var all = _history.GetAll();
            Assert.AreEqual("e12", all[0].Description);
            Assert.AreEqual("e8", all[4].Description);
        }

        // ─────────────── CopyAllInto (alloc-free) ───────────────
        // GetAll is now implemented in terms of CopyAllInto so the ring-walk
        // math has a single source of truth. The cases below cover the new
        // method directly so wrap-around / dest-reuse / null-dest cases are
        // pinned regardless of how callers compose them.

        [Test]
        public void CopyAllInto_EmptyHistory_ListIsEmpty()
        {
            var dest = new List<TransactionRecord>();
            _history.CopyAllInto(dest);
            Assert.AreEqual(0, dest.Count);
        }

        [Test]
        public void CopyAllInto_WithRecords_OrderedNewestFirst()
        {
            _history.Record(TransactionType.LoanPayment, "first", 10f, 1);
            _history.Record(TransactionType.LoanPayment, "second", 20f, 2);
            _history.Record(TransactionType.LoanPayment, "third", 30f, 3);

            var dest = new List<TransactionRecord>();
            _history.CopyAllInto(dest);

            Assert.AreEqual(3, dest.Count);
            Assert.AreEqual("third", dest[0].Description);
            Assert.AreEqual("second", dest[1].Description);
            Assert.AreEqual("first", dest[2].Description);
        }

        [Test]
        public void CopyAllInto_WithExistingItems_ClearsBeforeFilling()
        {
            _history.Record(TransactionType.LoanPayment, "rec", 10f, 1);

            var dest = new List<TransactionRecord>();
            // Pre-populate dest with stale data from a prior push
            dest.Add(new TransactionRecord(TransactionType.LoanPaidOff, "stale-a", 1f, 0));
            dest.Add(new TransactionRecord(TransactionType.LoanPaidOff, "stale-b", 2f, 0));

            _history.CopyAllInto(dest);

            Assert.AreEqual(1, dest.Count, "Stale entries must be cleared before filling.");
            Assert.AreEqual("rec", dest[0].Description);
        }

        [Test]
        public void CopyAllInto_DestSmallerCapacity_GrowsCapacity()
        {
            for (int i = 0; i < 4; i++)
                _history.Record(TransactionType.LoanPayment, $"e{i}", i, i);

            // Capacity 1 to force a grow.
            var dest = new List<TransactionRecord>(1);
            _history.CopyAllInto(dest);

            Assert.AreEqual(4, dest.Count);
            Assert.GreaterOrEqual(dest.Capacity, 4);
        }

        [Test]
        public void CopyAllInto_AfterWrap_ReturnsCorrectOrder()
        {
            // Capacity 5, add 7 entries → indexes 2..6 should remain.
            for (int i = 0; i < 7; i++)
                _history.Record(TransactionType.LoanPayment, $"e{i}", i * 10f, i);

            var dest = new List<TransactionRecord>();
            _history.CopyAllInto(dest);

            Assert.AreEqual(5, dest.Count);
            Assert.AreEqual("e6", dest[0].Description);
            Assert.AreEqual("e5", dest[1].Description);
            Assert.AreEqual("e4", dest[2].Description);
            Assert.AreEqual("e3", dest[3].Description);
            Assert.AreEqual("e2", dest[4].Description);
        }

        [Test]
        public void CopyAllInto_NullDest_NoOp()
        {
            _history.Record(TransactionType.LoanPayment, "rec", 10f, 1);

            // Must not throw.
            Assert.DoesNotThrow(() => _history.CopyAllInto(null));
        }

        [Test]
        public void GetAll_AfterRefactor_MatchesCopyAllInto()
        {
            // Sanity: GetAll is now a thin wrapper; both must produce the
            // exact same sequence of records.
            _history.Record(TransactionType.LoanPayment, "a", 10f, 1);
            _history.Record(TransactionType.CreditCardCharge, "b", 20f, 2);
            _history.Record(TransactionType.LoanPaidOff, "c", 30f, 3);

            var viaGetAll = _history.GetAll();
            var viaCopy = new List<TransactionRecord>();
            _history.CopyAllInto(viaCopy);

            Assert.AreEqual(viaGetAll.Count, viaCopy.Count);
            for (int i = 0; i < viaGetAll.Count; i++)
            {
                Assert.AreEqual(viaGetAll[i].Description, viaCopy[i].Description);
                Assert.AreEqual(viaGetAll[i].Tick, viaCopy[i].Tick);
            }
        }
    }
}
