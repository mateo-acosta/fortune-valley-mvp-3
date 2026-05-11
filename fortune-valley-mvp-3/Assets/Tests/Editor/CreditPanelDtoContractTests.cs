using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Tests
{
    // Contract test for the JSON shape that CreditWebBridge ships to the iframe.
    // The HTML side reads these field names verbatim (mockState absorber),
    // so a silent rename here would break the panel. These assertions are
    // the loud failure that should fire the next time someone tries.
    [TestFixture]
    public class CreditPanelDtoContractTests
    {
        [Test]
        public void CreditPanelDto_SerializesNewFieldNames()
        {
            var dto = new CreditPanelDTO
            {
                creditScore = 700,
                yearlyDebtPayment = 1234f,
                yearlyIncome = 108000f,
                cashOnHand = 5000f,
            };
            string json = JsonUtility.ToJson(dto);
            StringAssert.Contains("\"yearlyDebtPayment\":1234", json);
            StringAssert.Contains("\"yearlyIncome\":108000", json);
        }

        [Test]
        public void CreditPanelDto_DoesNotSerializeOldMonthlyDebtField()
        {
            string json = JsonUtility.ToJson(new CreditPanelDTO());
            Assert.IsFalse(
                json.Contains("monthlyDebtPayment"),
                "monthlyDebtPayment was renamed to yearlyDebtPayment; if you see this, the rename was reverted.");
        }

        [Test]
        public void LoanProductDto_SerializesNewFieldNames()
        {
            var dto = new LoanProductDTO
            {
                id = "loan_starter",
                termYears = 5,
                maxDtiRatio = 0.5f,
            };
            string json = JsonUtility.ToJson(dto);
            StringAssert.Contains("\"termYears\":5", json);
            StringAssert.Contains("\"maxDtiRatio\":0.5", json);
        }

        [Test]
        public void LoanProductDto_DoesNotSerializeTermMonths()
        {
            string json = JsonUtility.ToJson(new LoanProductDTO());
            Assert.IsFalse(
                json.Contains("termMonths"),
                "termMonths was renamed to termYears; the JS bridge expects termYears.");
        }

        [Test]
        public void ActiveLoanRowDto_SerializesNewFieldNames()
        {
            var dto = new ActiveLoanRowDTO
            {
                id = "loan-1",
                yearlyPayment = 1500f,
                yearsPaid = 2,
                termYears = 5,
            };
            string json = JsonUtility.ToJson(dto);
            StringAssert.Contains("\"yearlyPayment\":1500", json);
            StringAssert.Contains("\"yearsPaid\":2", json);
            StringAssert.Contains("\"termYears\":5", json);
        }

        [Test]
        public void ActiveLoanRowDto_DoesNotSerializeMonthlyFields()
        {
            string json = JsonUtility.ToJson(new ActiveLoanRowDTO());
            Assert.IsFalse(json.Contains("monthlyPayment"));
            Assert.IsFalse(json.Contains("monthsPaid"));
            Assert.IsFalse(json.Contains("termMonths"));
        }
    }
}
