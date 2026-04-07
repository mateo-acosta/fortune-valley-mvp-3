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
                }
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
