using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class UpdatePriceDeterminismTests
    {
        [Test]
        public void UpdatePrice_SameSeed_ProducesSamePrice()
        {
            var def1 = ScriptableObject.CreateInstance<InvestmentDefinition>();
            var def2 = ScriptableObject.CreateInstance<InvestmentDefinition>();

            // Set both to same config via reflection
            SetField(def1, "_displayName", "TestStock");
            SetField(def1, "_basePricePerShare", 100f);
            SetField(def1, "_annualReturnRate", 0.10f);
            SetField(def1, "_riskLevel", FortuneValley.Domain.Enums.RiskLevel.Medium);

            SetField(def2, "_displayName", "TestStock");
            SetField(def2, "_basePricePerShare", 100f);
            SetField(def2, "_annualReturnRate", 0.10f);
            SetField(def2, "_riskLevel", FortuneValley.Domain.Enums.RiskLevel.Medium);

            def1.InitializePrice();
            def2.InitializePrice();

            // Run 30 days with same seeds
            for (int day = 1; day <= 30; day++)
            {
                def1.SetDaySeed(day);
                def1.UpdatePrice();
                def2.SetDaySeed(day);
                def2.UpdatePrice();

                Assert.AreEqual(def1.CurrentPrice, def2.CurrentPrice, 0.001f,
                    $"Prices diverged on day {day}");
            }

            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(def2);
        }

        [Test]
        public void UpdatePrice_DifferentSeed_ProducesDifferentPrice()
        {
            var def1 = ScriptableObject.CreateInstance<InvestmentDefinition>();
            var def2 = ScriptableObject.CreateInstance<InvestmentDefinition>();

            SetField(def1, "_displayName", "StockA");
            SetField(def1, "_basePricePerShare", 100f);
            SetField(def1, "_annualReturnRate", 0.10f);
            SetField(def1, "_riskLevel", FortuneValley.Domain.Enums.RiskLevel.High);

            SetField(def2, "_displayName", "StockB");
            SetField(def2, "_basePricePerShare", 100f);
            SetField(def2, "_annualReturnRate", 0.10f);
            SetField(def2, "_riskLevel", FortuneValley.Domain.Enums.RiskLevel.High);

            def1.InitializePrice();
            def2.InitializePrice();

            for (int day = 1; day <= 30; day++)
            {
                def1.SetDaySeed(day);
                def1.UpdatePrice();
                def2.SetDaySeed(day);
                def2.UpdatePrice();
            }

            // Different names produce different seeds, so prices should differ
            // (at least after 30 volatile days)
            Assert.That(Mathf.Abs(def1.CurrentPrice - def2.CurrentPrice) > 0.01f,
                "Different seeds should produce different prices");

            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(def2);
        }

        [Test]
        public void UpdatePrice_FixedReturn_IsDeterministicWithoutSeed()
        {
            // Bonds use no randomness, so determinism is inherent
            var def = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetField(def, "_displayName", "TestBond");
            SetField(def, "_basePricePerShare", 100f);
            SetField(def, "_annualReturnRate", 0.04f);
            SetField(def, "_category", FortuneValley.Domain.Enums.InvestmentCategory.Bond);

            def.InitializePrice();

            for (int day = 1; day <= 10; day++)
            {
                def.SetDaySeed(day);
                def.UpdatePrice();
            }

            float price1 = def.CurrentPrice;

            // Reset and replay
            def.InitializePrice();
            for (int day = 1; day <= 10; day++)
            {
                def.SetDaySeed(day);
                def.UpdatePrice();
            }

            Assert.AreEqual(price1, def.CurrentPrice, 0.001f);

            Object.DestroyImmediate(def);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
        }
    }
}
