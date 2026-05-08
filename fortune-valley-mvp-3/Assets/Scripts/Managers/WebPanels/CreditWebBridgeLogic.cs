using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Reads loan-panel state off the live systems and writes it into
    /// the supplied DTO. Pure C# so EditMode tests can substitute small
    /// fakes for the system references.
    ///
    /// Maps:
    ///  - CreditScoreSystem -> creditScore (CC fields no longer flow)
    ///  - LoanSystem        -> totalDebt + monthlyDebtPayment + activeLoans + loanProducts
    ///  - CurrencyManager   -> cashOnHand
    ///  - CityManager       -> active loan lot names + availableLots[]
    /// </summary>
    public class CreditWebBridgeLogic : WebPanelBridgeLogic<CreditPanelDTO>
    {
        // LoanConfig.APR is stored in decimal form (0.15 = 15%). The HTML
        // panel renders it as a percent number (5.2 displays as "5.2%"),
        // so we multiply by 100 when populating the DTO.
        private const float DecimalToPercent = 100f;

        private LoanSystem _loanSystem;
        private CreditScoreSystem _creditCardSystem;
        private CurrencyManager _currencyManager;
        private CityManager _cityManager;
        private TransactionLog _transactionLog;
        private TimeManager _timeManager;

        // One-shot lot pre-selection. The bridge sets this when
        // OnLotLoanExploreRequested fires. PopulateDTO copies it to the DTO
        // and clears it so subsequent pushes do not re-fire the selection.
        private string _pendingSelectedLotId;

        public void SetSelectedLotId(string lotId)
        {
            _pendingSelectedLotId = lotId;
        }

        public void Initialize(
            LoanSystem loanSystem,
            CreditScoreSystem creditCardSystem,
            CurrencyManager currencyManager,
            CityManager cityManager,
            TransactionLog transactionLog,
            TimeManager timeManager)
        {
            _loanSystem = loanSystem;
            _creditCardSystem = creditCardSystem;
            _currencyManager = currencyManager;
            _cityManager = cityManager;
            _transactionLog = transactionLog;
            _timeManager = timeManager;
        }

        public override bool PopulateDTO(CreditPanelDTO target)
        {
            if (target == null) return false;
            // Hard requirements: loans + cash. CreditScoreSystem is optional
            // because the CC mechanic is being removed; when null, CC fields
            // emit zero rather than blocking the entire push.
            if (_loanSystem == null || _currencyManager == null) return false;

            // Home tab scalars. CC widgets were removed along with the CC
            // mechanic; only the score (driven by loan behavior + DTI) flows.
            target.creditScore = _creditCardSystem != null ? _creditCardSystem.CreditScore : 0;
            target.totalDebt = _loanSystem.TotalOutstandingPrincipal;
            target.monthlyDebtPayment = _loanSystem.TotalYearlyDebt;
            target.cashOnHand = _currencyManager.CheckingBalance;
            // creditScoreLabel left empty; HTML computes its own bucket from creditScore.

            // One-shot lot pre-selection: surface pending id then clear so the
            // iframe only acts on it once.
            target.selectedLotId = _pendingSelectedLotId;
            _pendingSelectedLotId = null;

            FillActiveLoans(target);
            FillLoanProducts(target);
            FillAvailableLots(target);
            FillHistory(target);
            return true;
        }

        // ───────────────────────── active loans ─────────────────────────

        private void FillActiveLoans(CreditPanelDTO target)
        {
            var portfolio = _loanSystem.Portfolio;
            var loans = portfolio != null ? portfolio.AllLoans : null;
            int count = 0;
            if (loans != null)
            {
                for (int i = 0; i < loans.Count; i++)
                {
                    if (loans[i] != null && loans[i].IsActive) count++;
                }
            }

            if (target.activeLoans == null || target.activeLoans.Length != count)
            {
                target.activeLoans = new ActiveLoanRowDTO[count];
            }

            int idx = 0;
            if (loans != null)
            {
                for (int i = 0; i < loans.Count; i++)
                {
                    var loan = loans[i];
                    if (loan == null || !loan.IsActive) continue;
                    if (target.activeLoans[idx] == null) target.activeLoans[idx] = new ActiveLoanRowDTO();
                    PopulateActiveLoan(target.activeLoans[idx], loan);
                    idx++;
                }
            }
        }

        private void PopulateActiveLoan(ActiveLoanRowDTO row, ActiveLoan loan)
        {
            row.id = loan.LoanId;
            row.lotName = ResolveLotName(loan.LotId);
            row.balance = loan.RemainingBalance;
            row.originalPrincipal = loan.Principal;
            row.monthlyPayment = loan.YearlyPayment;
            row.monthsPaid = loan.PaymentsMade;
            row.termMonths = loan.TermYears;
        }

        private string ResolveLotName(string lotId)
        {
            if (string.IsNullOrEmpty(lotId) || _cityManager == null) return lotId;
            var def = _cityManager.GetLot(lotId);
            return (def != null && !string.IsNullOrEmpty(def.DisplayName)) ? def.DisplayName : lotId;
        }

        // ───────────────────────── loan products ─────────────────────────

        private void FillLoanProducts(CreditPanelDTO target)
        {
            var configs = _loanSystem.AvailableLoans;
            int count = configs != null ? configs.Count : 0;

            if (target.loanProducts == null || target.loanProducts.Length != count)
            {
                target.loanProducts = new LoanProductDTO[count];
            }

            for (int i = 0; i < count; i++)
            {
                if (target.loanProducts[i] == null) target.loanProducts[i] = new LoanProductDTO();
                PopulateLoanProduct(target.loanProducts[i], configs[i]);
            }
        }

        private static void PopulateLoanProduct(LoanProductDTO row, LoanConfig cfg)
        {
            if (cfg == null) return;
            row.id = cfg.LoanId;
            row.name = cfg.DisplayName;
            row.apr = cfg.APR * DecimalToPercent;
            row.termMonths = cfg.TermYears;
            row.downPaymentPercent = cfg.DownPaymentPercent;
            row.minCreditScore = cfg.MinimumCreditScore;
            row.tagline = cfg.Tagline;
            // image left null; the iframe handles missing image gracefully.
        }

        // ───────────────────────── available lots ─────────────────────────

        private void FillAvailableLots(CreditPanelDTO target)
        {
            if (_cityManager == null)
            {
                target.availableLots = target.availableLots ?? new AvailableLotDTO[0];
                return;
            }

            var allLots = _cityManager.AllLots;
            int count = 0;
            if (allLots != null)
            {
                for (int i = 0; i < allLots.Count; i++)
                {
                    if (IsAvailable(allLots[i])) count++;
                }
            }

            if (target.availableLots == null || target.availableLots.Length != count)
            {
                target.availableLots = new AvailableLotDTO[count];
            }

            int idx = 0;
            if (allLots != null)
            {
                for (int i = 0; i < allLots.Count; i++)
                {
                    var lot = allLots[i];
                    if (!IsAvailable(lot)) continue;
                    if (target.availableLots[idx] == null) target.availableLots[idx] = new AvailableLotDTO();
                    target.availableLots[idx].id = lot.LotId;
                    target.availableLots[idx].name = lot.DisplayName;
                    target.availableLots[idx].price = lot.BaseCost;
                    idx++;
                }
            }
        }

        private bool IsAvailable(CityLotDefinition lot)
        {
            if (lot == null || string.IsNullOrEmpty(lot.LotId)) return false;
            var owner = _cityManager.GetOwner(lot.LotId);
            return owner != Owner.Player && owner != Owner.Rival;
        }

        // ───────────────────────── history (credit panel) ─────────────────────────

        private void FillHistory(CreditPanelDTO target)
        {
            if (_transactionLog == null || _transactionLog.History == null)
            {
                target.history = target.history ?? new HistoryEntryDTO[0];
                return;
            }

            var all = _transactionLog.History.GetAll();
            int count = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (IsCreditPanelType(all[i].Type)) count++;
            }

            if (target.history == null || target.history.Length != count)
            {
                target.history = new HistoryEntryDTO[count];
            }

            int ticksPerDay = _timeManager != null && _timeManager.EnginePulsesPerTick > 0 ? _timeManager.EnginePulsesPerTick : 1;
            int idx = 0;
            for (int i = all.Count - 1; i >= 0 && idx < count; i--)
            {
                var rec = all[i];
                if (!IsCreditPanelType(rec.Type)) continue;
                if (target.history[idx] == null) target.history[idx] = new HistoryEntryDTO();
                PopulateHistoryEntry(target.history[idx], rec, idx + 1, ticksPerDay);
                idx++;
            }
        }

        private static void PopulateHistoryEntry(HistoryEntryDTO row, TransactionRecord rec, int displayId, int ticksPerDay)
        {
            row.id = displayId;
            row.date = "Day " + (rec.Tick / ticksPerDay + 1);
            row.type = MapTransactionTypeToHtmlKey(rec.Type);
            row.description = rec.Description;
            row.amount = rec.Amount;
            row.sublabel = null;
        }

        private static bool IsCreditPanelType(TransactionType type)
        {
            switch (type)
            {
                case TransactionType.LoanOriginated:
                case TransactionType.LoanPayment:
                case TransactionType.LoanPaidOff:
                case TransactionType.LoanPaymentMissed:
                    return true;
                default:
                    return false;
            }
        }

        // Map C# TransactionType to the iframe's HISTORY_TYPES keys. Anything
        // not in the iframe's map will render with the raw key as the label.
        private static string MapTransactionTypeToHtmlKey(TransactionType type)
        {
            switch (type)
            {
                case TransactionType.LoanOriginated:    return "loan-originated";
                case TransactionType.LoanPayment:       return "loan-payment";
                case TransactionType.LoanPaidOff:       return "loan-payment";
                case TransactionType.LoanPaymentMissed: return "missed-payment";
                default:                                return "score-change";
            }
        }
    }
}
