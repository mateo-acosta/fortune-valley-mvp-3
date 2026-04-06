using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for DecisionDTOBuilder.
    /// Verifies the fluent builder produces correct DTOs.
    /// </summary>
    [TestFixture]
    public class DecisionDTOBuilderTests
    {
        private const string TestSessionId = "test-session-123";
        private const string TestGameMode = "homebase";

        [Test]
        public void Build_SetsSessionAndGameMode()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("test_type")
                .Build();

            Assert.AreEqual(TestSessionId, dto.session_id);
            Assert.AreEqual(TestGameMode, dto.game_mode);
        }

        [Test]
        public void Build_SetsDecisionType()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("loan_taken")
                .Build();

            Assert.AreEqual("loan_taken", dto.decision_type);
        }

        [Test]
        public void Build_SetsInstrumentId()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("lot_purchase")
                .Instrument("lot_downtown")
                .Build();

            Assert.AreEqual("lot_downtown", dto.instrument_id);
        }

        [Test]
        public void Build_SetsAmountAndDay()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("loan_payment")
                .Amount(500f)
                .Day(30)
                .Build();

            Assert.AreEqual(500f, dto.gross_amount);
            Assert.AreEqual(30, dto.in_game_day);
        }

        [Test]
        public void Build_SetsCategory()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("cc_payment")
                .Category("transfer")
                .Build();

            Assert.AreEqual("transfer", dto.category);
        }

        [Test]
        public void Build_WithLineItems_CreatesArray()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("cc_payment")
                .AddLineItem("checking", 100f, "outflow")
                .AddLineItem("credit", 100f, "inflow")
                .Build();

            Assert.IsNotNull(dto.line_items);
            Assert.AreEqual(2, dto.line_items.Length);
            Assert.AreEqual("checking", dto.line_items[0].account_affected);
            Assert.AreEqual(100f, dto.line_items[0].change_amount);
            Assert.AreEqual("outflow", dto.line_items[0].flow_category);
            Assert.AreEqual("credit", dto.line_items[1].account_affected);
        }

        [Test]
        public void Build_WithNoLineItems_ReturnsNullArray()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("franchise_upgrade")
                .Build();

            Assert.IsNull(dto.line_items);
        }

        [Test]
        public void Build_NullInstrument_SetsNull()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("franchise_upgrade")
                .Category("expense")
                .Build();

            Assert.IsNull(dto.instrument_id);
        }

        [Test]
        public void Build_FluentChain_ProducesCompleteDTO()
        {
            var dto = new DecisionDTOBuilder(TestSessionId, TestGameMode)
                .Type("loan_taken")
                .Instrument("lot1")
                .Amount(8000f)
                .Day(15)
                .Category("transfer")
                .AddLineItem("checking", -2000f, "outflow")
                .Build();

            Assert.AreEqual(TestSessionId, dto.session_id);
            Assert.AreEqual(TestGameMode, dto.game_mode);
            Assert.AreEqual("loan_taken", dto.decision_type);
            Assert.AreEqual("lot1", dto.instrument_id);
            Assert.AreEqual(8000f, dto.gross_amount);
            Assert.AreEqual(15, dto.in_game_day);
            Assert.AreEqual("transfer", dto.category);
            Assert.AreEqual(1, dto.line_items.Length);
            Assert.AreEqual(-2000f, dto.line_items[0].change_amount);
        }
    }
}
