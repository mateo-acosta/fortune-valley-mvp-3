using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class GamePlayerStateDTOTests
    {
        [Test]
        public void RoundTrip_AllFieldsSurviveSerialization()
        {
            var original = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                current_day = 42,
                checking_balance = 1500.50f,
                credit_balance = 350.25f,
                investment_balance = 5000f,
                credit_score = 720,
                budget_variance_streak = 3,
                tax_liability_ytd = 0f,
                lots_owned = new[] { "lot_1", "lot_3" },
                rival_lots_owned = new[] { "lot_2" },
                learning_levels_completed = new[] { "level_1" },
                consecutive_insolvent_months = 1,
                bankruptcy_flag = false,
                restaurant_level = 3,
                current_tick = 420,
                active_loans = new[]
                {
                    new ActiveLoanDTO
                    {
                        loan_id = "loan_1",
                        lot_id = "lot_3",
                        principal = 8000f,
                        remaining_balance = 6500f,
                        monthly_payment = 700f,
                        payments_made = 2,
                        term_months = 12,
                        apr = 0.08f,
                        down_payment = 2000f,
                        start_day = 10
                    }
                },
                insurance_policies = new[]
                {
                    new ActiveInsurancePolicyDTO
                    {
                        policy_id = "general_1",
                        lot_id = "lot_1",
                        policy_type = "GeneralProtection",
                        monthly_premium = 25f,
                        deductible = 100f,
                        start_day = 5
                    }
                },
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO
                    {
                        building_id = "restaurant",
                        daily_payout = 100f,
                        ticks_remaining = 7,
                        is_ready = false
                    },
                    new PendingIncomeEntryDTO
                    {
                        building_id = "lot_A",
                        daily_payout = 50f,
                        ticks_remaining = 0,
                        is_ready = true
                    }
                },
                schema_version = 1
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GamePlayerStateDTO>(json);

            // Scalar fields
            Assert.AreEqual("homebase", restored.game_mode);
            Assert.AreEqual(42, restored.current_day);
            Assert.AreEqual(1500.50f, restored.checking_balance, 0.01f);
            Assert.AreEqual(350.25f, restored.credit_balance, 0.01f);
            Assert.AreEqual(5000f, restored.investment_balance, 0.01f);
            Assert.AreEqual(720, restored.credit_score);
            Assert.AreEqual(1, restored.consecutive_insolvent_months);
            Assert.AreEqual(false, restored.bankruptcy_flag);
            Assert.AreEqual(3, restored.restaurant_level);
            Assert.AreEqual(420, restored.current_tick);

            // Array fields
            Assert.AreEqual(2, restored.lots_owned.Length);
            Assert.AreEqual("lot_1", restored.lots_owned[0]);
            Assert.AreEqual(1, restored.rival_lots_owned.Length);

            // Loan DTO
            Assert.AreEqual(1, restored.active_loans.Length);
            Assert.AreEqual("loan_1", restored.active_loans[0].loan_id);
            Assert.AreEqual("lot_3", restored.active_loans[0].lot_id);
            Assert.AreEqual(8000f, restored.active_loans[0].principal, 0.01f);
            Assert.AreEqual(6500f, restored.active_loans[0].remaining_balance, 0.01f);
            Assert.AreEqual(700f, restored.active_loans[0].monthly_payment, 0.01f);
            Assert.AreEqual(2, restored.active_loans[0].payments_made);
            Assert.AreEqual(12, restored.active_loans[0].term_months);
            Assert.AreEqual(0.08f, restored.active_loans[0].apr, 0.001f);

            // Insurance DTO
            Assert.AreEqual(1, restored.insurance_policies.Length);
            Assert.AreEqual("general_1", restored.insurance_policies[0].policy_id);
            Assert.AreEqual("lot_1", restored.insurance_policies[0].lot_id);
            Assert.AreEqual("GeneralProtection", restored.insurance_policies[0].policy_type);
            Assert.AreEqual(25f, restored.insurance_policies[0].monthly_premium, 0.01f);

            // Pending income DTO
            Assert.AreEqual(2, restored.pending_incomes.Length);
            Assert.AreEqual("restaurant", restored.pending_incomes[0].building_id);
            Assert.AreEqual(100f, restored.pending_incomes[0].daily_payout, 0.01f);
            Assert.AreEqual(7, restored.pending_incomes[0].ticks_remaining);
            Assert.IsFalse(restored.pending_incomes[0].is_ready);
            Assert.AreEqual("lot_A", restored.pending_incomes[1].building_id);
            Assert.AreEqual(50f, restored.pending_incomes[1].daily_payout, 0.01f);
            Assert.AreEqual(0, restored.pending_incomes[1].ticks_remaining);
            Assert.IsTrue(restored.pending_incomes[1].is_ready);

            // Schema version
            Assert.AreEqual(1, restored.schema_version);
        }

        [Test]
        public void LegacyJson_WithoutSchemaField_DeserializesToZero()
        {
            // Unity JsonUtility drops unknown keys and fills missing keys with
            // type defaults. A pre-change JSON (no schema_version) must parse
            // to schema_version == 0 so the service takes the migration path.
            string legacyJson =
                "{\"game_mode\":\"homebase\",\"current_day\":1," +
                "\"pending_incomes\":[{\"building_id\":\"lot_A\",\"accumulated\":42.0,\"ready_amount\":0.0,\"full_day_amount\":100.0,\"is_ready\":false}]}";

            var restored = JsonUtility.FromJson<GamePlayerStateDTO>(legacyJson);

            Assert.AreEqual(0, restored.schema_version);
            Assert.IsNotNull(restored.pending_incomes);
            Assert.AreEqual(1, restored.pending_incomes.Length);
            Assert.AreEqual("lot_A", restored.pending_incomes[0].building_id);
            // Legacy numeric fields are dropped on deserialize; new fields are zero.
            Assert.AreEqual(0f, restored.pending_incomes[0].daily_payout);
            Assert.AreEqual(0, restored.pending_incomes[0].ticks_remaining);
        }

        [Test]
        public void RoundTrip_NullArrays_DoNotCrash()
        {
            var original = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                current_day = 1
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GamePlayerStateDTO>(json);

            Assert.AreEqual("homebase", restored.game_mode);
            Assert.AreEqual(1, restored.current_day);
            // Null arrays become empty arrays after round-trip (JsonUtility behavior)
        }

        [Test]
        public void RoundTrip_EmptyArrays_SurviveSerialization()
        {
            var original = new GamePlayerStateDTO
            {
                active_loans = new ActiveLoanDTO[0],
                insurance_policies = new ActiveInsurancePolicyDTO[0],
                lots_owned = new string[0]
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GamePlayerStateDTO>(json);

            Assert.IsNotNull(restored.active_loans);
            Assert.AreEqual(0, restored.active_loans.Length);
            Assert.IsNotNull(restored.insurance_policies);
            Assert.AreEqual(0, restored.insurance_policies.Length);
        }
    }
}
