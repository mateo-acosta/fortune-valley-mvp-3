/* ============================================================
   Fortune Valley Term Definitions

   Single source of truth for every "what is this?" explanation
   shown in the in-game web overlay panels. Each entry has the
   same shape:

     { label, what, why, how }                     standard term
     { label, what, why, how, subsections: [...] } term that has
                                                    a list of
                                                    sub-items
                                                    (e.g. category
                                                    listing each
                                                    category type,
                                                    history_legend
                                                    listing each
                                                    row type)

   Tone rules baked in:
     - "you" / "your", never "the user"
     - 2 to 4 sentences per section, hard cap
     - one concrete number per section when it helps
     - no em dashes, no exclamation marks, no emojis
   ============================================================ */

export const TERM_DEFINITIONS = {

  /* ============================================================
     CARD-LEVEL EXPLAINERS
     One per card surface. Lives next to the card title, so it
     answers "what is this whole tile showing me" before the
     student dives into the individual numbers inside it.
     ============================================================ */

  "card:loans_summary": {
    label: "Loans Card",
    what:  "This card pulls together every loan you currently owe in one place. " +
           "It adds up your total debt and the cash that leaves your account every month to keep those loans current.",
    why:   "Knowing what you owe in total, and what it costs you each month, is the starting point for every other money decision. " +
           "If your monthly debt payments climb past about 30 percent of your income, you'll feel squeezed every month and have less to invest or save.",
    how:   "These numbers update automatically as you take out new loans on the Explore tab, make scheduled payments, or pay off a loan in full. " +
           "You can see each individual loan in the Active Loans list below this card."
  },

  "card:credit_card_summary": {
    label: "Credit Card Card",
    what:  "This card gathers everything about your credit card in one view: your score, how much of your limit you're using, your balance, and how much room you have left. " +
           "Think of it as the dashboard for your borrowing power.",
    why:   "Your credit card is the cheapest way to borrow short term if you pay it off in full each month, and the most expensive way to borrow if you don't. " +
           "Watching this card is how you spot the difference before it costs you.",
    how:   "Spending on the card raises your balance and utilization. " +
           "Paying the card down at the end of each month lowers them. " +
           "All four numbers move together based on those two actions."
  },

  "card:active_loan_row": {
    label: "Active Loan Row",
    what:  "Each row here is one loan you're currently paying off. " +
           "It shows the loan name, how many months you've paid out of the total term, what's still owed, the monthly payment, and a bar that fills up as you pay down the principal.",
    why:   "Seeing each loan separately helps you spot which one is costing you the most each month and which one you're closest to finishing. " +
           "Paying off the smallest loan first frees up cash quickly; paying off the highest rate loan first saves you the most money over time.",
    how:   "A row appears as soon as you finance a lot from the Explore tab. " +
           "It disappears when the bar reaches 100 percent and the loan is paid in full."
  },

  "card:loan_product": {
    label: "Loan Product",
    what:  "A loan product is one specific deal the bank is offering: a set rate, a set length, and a set down payment percentage. " +
           "Different products fit different situations, like a 30 year loan for low monthly payments versus a 10 year loan for low total cost.",
    why:   "The product you pick changes the math for years. " +
           "A 30 year loan and a 15 year loan on the same lot can differ by tens of thousands of dollars by the time you finish paying.",
    how:   "Use the arrows on this card to flip through every product you currently qualify for. " +
           "The Stats card next to it updates instantly so you can compare monthly payment and total cost side by side."
  },

  "card:loan_offer": {
    label: "Loan Offer",
    what:  "This card shows the full math on the loan product you've got selected, applied to the lot you've got selected. " +
           "Down payment, monthly payment, total loan, total cost: every number a real lender would put on a sheet of paper.",
    why:   "Borrowing without seeing the full cost is how people end up paying double for a property. " +
           "Reading every line on this card before you tap Apply Now is the single best habit you can build in this game.",
    how:   "Change the lot in the dropdown above, or flip the loan product in the carousel, and every number here updates. " +
           "The qualification line at the bottom tells you whether you'll be approved before you commit."
  },

  "card:investing_balance": {
    label: "Investing Balance Card",
    what:  "This card shows the cash sitting inside your Investing account that hasn't been put to work yet. " +
           "It's money you've moved over from Checking but haven't used to buy any investments.",
    why:   "Cash in this account doesn't earn anything on its own. " +
           "If you let too much sit here for too long, you're missing out on returns you could be earning if you actually bought something with it.",
    how:   "Buying shares on the Trade tab pulls money out of this balance. " +
           "Selling shares puts money back in. " +
           "Topping up from your main Checking balance also raises this number."
  },

  "card:invested_total": {
    label: "Invested Total Card",
    what:  "This card shows the total market value of every investment you currently own, added together. " +
           "If you own 3 shares worth $100 each and 2 shares worth $50 each, this number is $400.",
    why:   "This is the real measure of how big your portfolio is right now. " +
           "Watching it grow over weeks and months is the clearest signal that your investing strategy is working.",
    how:   "It rises when share prices rise or when you buy more shares. " +
           "It falls when prices drop or when you sell. " +
           "The graph below this card shows how it has moved over the last 30 days."
  },

  "card:total_gain": {
    label: "Total Gain Card",
    what:  "This card shows your lifetime profit (or loss) from investing: every dollar you've made or lost since you started, added up. " +
           "If you put in $1,000 and your investments are now worth $1,200, your total gain is $200.",
    why:   "Total gain is the answer to the question \"is investing actually working for me?\" " +
           "Green is profit, red is loss. " +
           "Over months and years you want this number trending up, even if it dips on bad weeks.",
    how:   "Every share you sell at a profit adds to this number. " +
           "Every share that drops in price below what you paid pushes it down. " +
           "Holding through a rough patch is often how a red number turns green again."
  },

  "card:risk_profile": {
    label: "Risk Profile Card",
    what:  "This card sums up how risky your overall portfolio is, based on the mix of low, medium, and high risk holdings inside it. " +
           "It blends them by how much money you have in each, not by how many holdings you have.",
    why:   "A portfolio rated High risk can grow fast but also crash hard. " +
           "A Low risk portfolio grows slowly but rarely loses big chunks. " +
           "Knowing where your portfolio sits helps you sleep at night when the market is choppy.",
    how:   "Buying high risk holdings (like crypto or volatile tech stocks) pushes the rating toward High. " +
           "Buying bonds or stable food and real estate stocks pulls it back toward Low. " +
           "You can see the badge update in real time as you trade."
  },

  "card:portfolio_value_history": {
    label: "Portfolio Value Graph",
    what:  "This graph plots the total value of your investments over the last 30 days. " +
           "Each point on the line is what your portfolio was worth on that day.",
    why:   "Charts like this show you the shape of investing over time, not just a single snapshot. " +
           "You'll notice that good portfolios tend to wander up and down day to day but trend upward over weeks, which is normal and expected.",
    how:   "Every time the in-game market ticks (about once a day in game time), a new point gets added to the right edge. " +
           "Big jumps usually mean a holding had a sharp price move. " +
           "Drops aren't a reason to panic, they're often just normal market noise."
  },

  "card:current_holdings": {
    label: "Current Holdings",
    what:  "This list shows every investment you currently own, with how many shares you hold and what each holding is worth right now. " +
           "It's the inventory of your investing account.",
    why:   "Seeing all your holdings together is how you spot whether you're spread across different types of investments or piled into just one or two. " +
           "Putting most of your money into a single stock is much riskier than splitting it across several different ones.",
    how:   "Buying or selling on the Trade tab adds, removes, or resizes rows here. " +
           "The Portfolio tab gives you a richer version of this list with filters and per-holding detail."
  },

  "card:holding_detail": {
    label: "Holding Detail",
    what:  "This card zooms in on one investment you own and shows the math behind it: shares owned, average cost, current value, and total gain. " +
           "It also charts that one investment's price over the last 30 days.",
    why:   "Knowing the detail on each holding helps you decide whether to hold, buy more, or sell. " +
           "If your average cost is well below the current price, you're sitting on a profit; if it's well above, you're sitting on a loss.",
    how:   "Tap any row in the Holdings list to load it here. " +
           "Use the Trade this button to jump to the Trade tab and buy or sell shares of this one investment."
  },

  "card:investment": {
    label: "Investment Card",
    what:  "Each card in this grid is one investment you can buy. " +
           "It shows the name, the current price, today's price change in percent, and how risky the investment is rated.",
    why:   "Comparing cards side by side is the fastest way to spot the differences between investments. " +
           "A card with a small green change percent and a Low risk badge is very different from one with a big green change percent and a High risk badge.",
    how:   "Tap a card to open the Trade tab loaded with that investment, where you can see its price history and place a buy. " +
           "Use the Category and Industry chips above the grid to narrow down what you're looking at."
  },

  "card:trade_panel": {
    label: "Trade Panel",
    what:  "This is where you actually buy or sell shares of the investment you've got selected. " +
           "The card on the right shows the price history; the card on the left shows the buy and sell controls and the current state of your position.",
    why:   "Trading without seeing both the price chart and your own current position is how people buy at the worst time and sell at the worst time. " +
           "Having both side by side helps you slow down and check whether the trade actually makes sense.",
    how:   "Tap Buy 1 share to spend money from your Checking balance and add a share. " +
           "Tap Sell 1 share to convert one share back into cash at the current price."
  },

  /* ============================================================
     CREDIT TERMS
     ============================================================ */

  "total_debt": {
    label: "Total Debt",
    what:  "Total debt is the full amount of money you currently owe across all your loans, added up. " +
           "If you owe $24,800 on a Bistro Lot loan and $23,400 on a Corner Lot loan, your total debt is $48,200.",
    why:   "Lenders look at this number when deciding whether to give you another loan, and it directly affects your credit score. " +
           "More importantly, every dollar of debt costs you interest until it's paid off, so a smaller total debt means more money in your pocket each month.",
    how:   "It goes up when you take out a new loan in the Explore tab and goes down a little every month as your scheduled payments chip away at the principal. " +
           "It does not include your credit card balance, which lives in its own card."
  },

  "monthly_payment": {
    label: "Monthly Payment",
    what:  "Your monthly payment is the cash that automatically leaves your account each month to keep your loans current. " +
           "On a $100,000 loan at 5 percent over 30 years, that payment is about $540 a month.",
    why:   "This is the number that hits your wallet every single month, no matter what else is happening. " +
           "If your monthly payments climb above what you actually earn, you go insolvent and risk bankruptcy.",
    how:   "Each loan you take on has its own monthly payment, and they add up to the total shown here. " +
           "The longer the loan term and the higher the APR, the more of your monthly cash gets eaten up."
  },

  "credit_score": {
    label: "Credit Score",
    what:  "Your credit score is a number from about 300 to 850 that tells lenders how likely you are to pay them back. " +
           "Above 700 is Good, 650 to 699 is Fair, below 650 is Poor.",
    why:   "A higher score unlocks loans with lower APRs, which can save you tens of thousands of dollars over a 30 year loan. " +
           "A low score means worse loan terms, smaller credit limits, and sometimes no loan offer at all.",
    how:   "Paying every loan and credit card bill on time pushes your score up over months. " +
           "Missing a payment, maxing out your card, or going through bankruptcy pulls it down quickly. " +
           "The score updates in the Credit panel as those events happen."
  },

  "credit_utilization": {
    label: "Credit Utilization",
    what:  "Credit utilization is the share of your credit card limit you're currently using. " +
           "If your limit is $5,000 and your balance is $1,250, your utilization is 25 percent. " +
           "Lenders watch this number to judge how stretched you are.",
    why:   "Keeping utilization under 30 percent protects your credit score. " +
           "Once you cross 50 percent, lenders read it as a sign you might miss a payment, and your score can drop quickly even if you've never been late.",
    how:   "The bar on the Credit Card card turns yellow at 30 percent and red at 50 percent. " +
           "Pay down your balance, or qualify for a higher limit, to push the bar back into the green. " +
           "Keeping it green is the fastest lever you have on your credit score in this game."
  },

  "cc_balance": {
    label: "CC Balance",
    what:  "Your credit card balance is the amount you currently owe on the card. " +
           "Every time you put a charge on the card, this number goes up. " +
           "Every time you pay the card, this number goes down.",
    why:   "Carrying a balance from one month to the next is how credit card debt snowballs, because the bank charges interest (often 20 percent or more per year) on whatever you don't pay off. " +
           "Paying it to zero every month is how a credit card stays free instead of expensive.",
    how:   "When the monthly statement closes, whatever balance you haven't paid starts charging interest. " +
           "Pay the balance down before then to skip that interest entirely."
  },

  "available_credit": {
    label: "Available Credit",
    what:  "Available credit is the room you still have left on your card before you hit the limit. " +
           "If your limit is $5,000 and your balance is $1,240, you have $3,760 of available credit.",
    why:   "Having room on your card is useful for surprise expenses and emergencies. " +
           "It also means your utilization is low, which protects your credit score.",
    how:   "Spending lowers it, paying raises it. " +
           "Qualifying for a higher overall limit also raises it without you needing to pay anything."
  },

  "loan_principal_remaining": {
    label: "Outstanding (Loan Balance)",
    what:  "This is the amount of the original loan you still owe. " +
           "If you borrowed $28,000 and have paid back $3,200 worth of principal, your outstanding balance is $24,800.",
    why:   "This is the number the lender actually cares about. " +
           "Until it hits zero, the loan keeps charging you interest and your monthly payments keep coming out.",
    how:   "Each monthly payment splits between interest (the bank's fee) and principal (what you actually owe). " +
           "Only the principal portion shrinks this number, which is why the outstanding balance drops slowly at the start of a loan and faster near the end."
  },

  "loan_paid_off": {
    label: "Paid Off",
    what:  "This bar shows how much of the original loan you've paid down so far, as a percent. " +
           "100 percent means the loan is gone.",
    why:   "It's a fast visual check of how close you are to being free of one of your debts. " +
           "Watching it crawl up over months is one of the most concrete signs of financial progress in the game.",
    how:   "It moves forward only as your principal shrinks. " +
           "If most of your monthly payment is going to interest (which is normal early in a loan), this bar moves slowly at first and speeds up later."
  },

  "apr": {
    label: "APR (Annual Percentage Rate)",
    what:  "APR is the yearly cost of borrowing money, written as a percent. " +
           "On a $10,000 loan with a 6 percent APR, the lender charges you about $600 per year in interest before any fees. " +
           "Lower APR is cheaper for you, higher APR is more expensive.",
    why:   "APR is the single biggest reason a loan ends up costing far more than the price tag. " +
           "A 30 year loan at 7 percent APR can cost you almost double the home price by the time it's paid off, so even a 1 percent difference in APR is worth shopping around for.",
    how:   "In Fortune Valley, each loan product on the Explore tab shows its own APR. " +
           "Better credit scores unlock loans with lower APRs, which means smaller monthly payments and a smaller total cost on the same lot."
  },

  "min_credit_score": {
    label: "Min Credit Score",
    what:  "This is the lowest credit score the lender will accept for this specific loan. " +
           "If the minimum is 700 and your score is 680, you don't qualify for this product, even if you have the cash.",
    why:   "Lenders set minimums to protect themselves from people likely to miss payments. " +
           "The best loans (lowest APR, smallest down payment) usually require the highest scores, which is the practical reward for keeping your credit healthy.",
    how:   "Build your score by paying on time, keeping utilization low, and avoiding missed payments. " +
           "When your score crosses a threshold, new loan products unlock automatically in the carousel."
  },

  "down_payment": {
    label: "Down Payment",
    what:  "A down payment is cash you pay up front when you buy a property, taken out of your own savings instead of borrowed. " +
           "On a $100,000 lot with a 20 percent down payment, you pay $20,000 in cash and borrow the other $80,000.",
    why:   "A bigger down payment means a smaller loan, which means a smaller monthly payment and far less interest over the life of the loan. " +
           "It also makes lenders more comfortable, so it can unlock better APRs.",
    how:   "The Stats card shows the exact dollar amount and percent for the loan you've got selected. " +
           "If you don't have that much cash on hand, the Apply Now button is disabled and the qualify line tells you to come back when you do."
  },

  "loan_term": {
    label: "Term (Loan Term)",
    what:  "The term is how long the loan lasts, usually written in years. " +
           "A 30 year loan stretches your payments out over 30 years; a 10 year loan crams them into 10.",
    why:   "Longer terms mean smaller monthly payments but more years of paying interest, so the total cost ends up much higher. " +
           "Shorter terms mean bigger monthly payments but a smaller total cost. " +
           "It's a direct trade between cash flow now and total dollars paid later.",
    how:   "Each loan product in the carousel has a fixed term. " +
           "Picking a different product changes the term, which is why the monthly payment and total cost on the Stats card jump when you flip between products."
  },

  "total_loan": {
    label: "Total Loan",
    what:  "Total loan is the amount the bank actually lends you: the price of the lot minus your down payment. " +
           "On a $100,000 lot with a $20,000 down payment, the total loan is $80,000.",
    why:   "This is the principal you'll be paying interest on for the entire term. " +
           "A smaller total loan, achieved by saving more for a bigger down payment, directly cuts your monthly payment and your lifetime interest.",
    how:   "It's automatically calculated from the lot price and the loan product's down payment percentage. " +
           "There's no separate decision to make here, but watching how it changes when you swap products is a useful sanity check."
  },

  "total_cost": {
    label: "Total Cost",
    what:  "Total cost is what the lot actually costs you in the end: your down payment plus every monthly payment you'll make over the full term. " +
           "On a 30 year loan, this is often nearly double the price of the lot itself.",
    why:   "The price tag of a lot is not the price you pay. " +
           "Total cost is. " +
           "Comparing total cost across loan products is the fairest way to see which one is genuinely cheaper, and it often picks a different winner than the lowest monthly payment.",
    how:   "It updates automatically when you change the lot or the loan product. " +
           "A shorter term or a lower APR will both bring this number down."
  },

  "debt_to_income": {
    label: "Debt-to-Income (DTI)",
    what:  "Debt-to-income is the share of your monthly earnings that's already locked up in debt payments. " +
           "If you earn $4,000 a month and owe $1,800 a month across all loans, your DTI is 45 percent.",
    why:   "Lenders use DTI to judge whether you can actually afford another loan on top of what you already owe. " +
           "Most lenders cap DTI around 45 percent, which is why a new loan can be denied even if your credit score is fine.",
    how:   "The qualify line on the Loan Offer card flags this when it kicks in. " +
           "To bring DTI down, either pay off existing loans, increase your monthly income, or pick a cheaper lot or loan product."
  },

  "history_legend": {
    label: "Recent Activity",
    what:  "This list is a running log of everything credit-related that has happened to your account: loans starting and ending, statements closing, payments going through, missed payments, and credit score changes.",
    why:   "Money problems usually start small and build up unnoticed. " +
           "Skimming this list once in a while is how you spot patterns (like a missed payment that dropped your score) and learn from your own decisions.",
    how:   "New entries appear automatically as events happen in the game. " +
           "Use the date filter pills above to narrow down to the last 30 or 90 days when the list gets long.",
    subsections: [
      { name: "Loan Payment", desc: "Money that came out of your account to make a scheduled payment on one of your loans." },
      { name: "Loan Originated", desc: "The day a new loan started. The amount shown is the principal the bank lent you." },
      { name: "CC Statement", desc: "The credit card billing cycle closed and the bank tallied up everything you owed that month." },
      { name: "CC Payment", desc: "Money you sent to the credit card to pay down the balance." },
      { name: "Missed Payment", desc: "A scheduled payment didn't go through. This is a serious event: it triggers fees and can drop your credit score sharply." },
      { name: "Score Change", desc: "Your credit score moved up or down because of recent activity. The sublabel shows the change and the new score." }
    ]
  },

  /* ============================================================
     INVESTING TERMS
     ============================================================ */

  "investing_balance": {
    label: "Balance (Investing Cash)",
    what:  "This is cash that lives inside your Investing account but hasn't been used to buy anything yet. " +
           "It's separate from your main Checking balance.",
    why:   "Cash sitting in this balance is not earning anything. " +
           "It's only useful as ammunition for buying investments. " +
           "Most of the time, you want this number small and your invested total large.",
    how:   "It goes up when you transfer money in from Checking or sell a holding. " +
           "It goes down when you buy a holding on the Trade tab."
  },

  "invested_total": {
    label: "Invested",
    what:  "Invested is the current market value of every share, bond, and fund you currently own, added together. " +
           "It changes every time prices move, even if you don't trade.",
    why:   "This is the real size of your portfolio. " +
           "Watching it grow over time, especially when you compare it to the total you've put in, is how you tell if your investing strategy is actually paying off.",
    how:   "Buying more raises it (and lowers your cash balance). " +
           "Selling shrinks it. " +
           "Day to day price moves push it up and down on their own."
  },

  "total_gain": {
    label: "Total Gain",
    what:  "Total gain is your lifetime profit (or loss) from investing, in dollars. " +
           "If you've put in $5,000 over time and your portfolio is now worth $5,420, your total gain is $420.",
    why:   "This is the bottom-line answer to \"is this working?\" " +
           "Green is profit. " +
           "Red is loss. " +
           "Long term, you want it green and growing, even if it dips on bad days.",
    how:   "Every share you sell at a price above what you paid adds to total gain. " +
           "Every share whose price drops below your average cost subtracts from it (until you sell or it bounces back)."
  },

  "risk": {
    label: "Risk Level",
    what:  "Risk level tells you how much the price of an investment can swing up or down. " +
           "Low risk means small, steady changes (a few percent a month). " +
           "High risk means the price can jump or crash by 20 percent or more in a short window.",
    why:   "Higher risk usually means higher possible reward, and also higher possible loss. " +
           "Putting all your money into one high risk stock can double your portfolio, or cut it in half. " +
           "Spreading money across different risk levels is how real investors avoid going broke on one bad bet.",
    how:   "Each investment in Explore shows a Low, Medium, or High badge. " +
           "Your portfolio's overall risk on the Home tab is a weighted blend of everything you own. " +
           "If most of your money is in High risk holdings, expect the green graph to swing hard in both directions."
  },

  "category": {
    label: "Category",
    what:  "Category is the broad type of investment: stocks, ETFs, bonds, or T-Bills. " +
           "Each one works differently and has its own typical mix of risk and return.",
    why:   "Mixing categories is one of the simplest ways to lower the overall risk of your portfolio. " +
           "Stocks can grow fast but crash hard; bonds and T-Bills grow slowly but stay steady; ETFs sit in between. " +
           "Owning a bit of each smooths out the ride.",
    how:   "Use the chips above the grid to filter to one category at a time. " +
           "On the Home tab, your Risk Profile already reflects whatever mix of categories you've bought.",
    subsections: [
      { name: "Stock",  desc: "A small piece of ownership in one specific company. Stocks can grow fast (or shrink fast) based on how that company is doing." },
      { name: "ETF",    desc: "A bundle that holds many stocks at once, like a basket. Buying one ETF gives you a slice of dozens of companies, which spreads out your risk." },
      { name: "Bond",   desc: "A loan you make to a company or government. They pay you back with interest over time. Bonds are usually steadier than stocks but grow more slowly." },
      { name: "T-Bill", desc: "A very short term loan to the government. T-Bills are the safest investment in the game, with small but reliable returns." }
    ]
  },

  "industry": {
    label: "Industry",
    what:  "Industry is the kind of business an investment is in: tech, energy, food, real estate, and so on. " +
           "Stocks and some bonds are tied to one specific industry; ETFs and T-Bills usually aren't.",
    why:   "Industries rise and fall together. " +
           "If oil prices crash, every energy stock tends to drop the same week. " +
           "Spreading your money across different industries means one bad week in one industry won't take down your whole portfolio.",
    how:   "Use the chips above the grid to filter the list to one industry at a time. " +
           "Try buying across at least three different industries to keep your portfolio from leaning too hard on any single one.",
    subsections: [
      { name: "Technology",     desc: "Companies that make software, computer hardware, or internet services. They can grow very fast in good years and drop sharply in bad ones." },
      { name: "Financials",     desc: "Banks, insurance companies, and other money businesses. Their performance tracks closely with how the broader economy is doing." },
      { name: "Energy",         desc: "Oil, gas, and renewable power companies. Their prices swing with the cost of fuel and electricity, which can be very up and down." },
      { name: "Consumer Goods", desc: "Companies that make everyday products people keep buying no matter what, like toothpaste and groceries. Usually steadier than tech or energy." },
      { name: "Healthcare",     desc: "Hospitals, medicine makers, and health insurers. People need healthcare in good times and bad, so this industry tends to be fairly stable." },
      { name: "Industrials",    desc: "Companies that build big physical things like trucks, factories, and machinery. They tend to do well when the economy is growing." },
      { name: "Food",           desc: "Restaurants, grocery brands, and food producers. Closely tied to what people are spending on meals every day." },
      { name: "Real Estate",    desc: "Companies that own buildings and land, including the bonds funded by Fortune Valley itself. Often steady, with returns tied to rents and property values." }
    ]
  },

  "shares_owned": {
    label: "Shares Owned",
    what:  "The number of individual shares of this investment you currently hold. " +
           "If you own 5 shares of a stock priced at $200, the holding is worth $1,000.",
    why:   "It's the basic unit of investing. " +
           "Buying more shares means more money invested and more potential gain or loss. " +
           "Selling shares converts them back into cash.",
    how:   "Every Buy on the Trade tab adds shares; every Sell removes them. " +
           "When the count hits zero, the holding disappears from your Portfolio."
  },

  "avg_cost": {
    label: "Average Cost",
    what:  "Your average cost is the average price you paid for the shares you currently own. " +
           "If you bought 2 shares at $100 and 2 shares at $150, your average cost is $125 per share.",
    why:   "Comparing average cost to the current price tells you whether you're up or down on this holding. " +
           "If the price is above your average cost, selling locks in a profit; if it's below, selling locks in a loss.",
    how:   "Buying more shares at a different price reshuffles this number. " +
           "Selling doesn't change the average cost of the shares you still hold."
  },

  "current_value": {
    label: "Current Value",
    what:  "Current value is what your shares of this one investment would be worth if you sold them right now. " +
           "It's just shares owned times the current price.",
    why:   "It tells you how much of your portfolio is currently riding on this one holding. " +
           "If a single holding is most of your portfolio, you're heavily exposed to whatever happens to that one investment.",
    how:   "It moves up and down every time the share price moves, even if you do nothing. " +
           "Buying more shares raises it, selling lowers it."
  },

  "price": {
    label: "Price",
    what:  "Price is how much one share of this investment costs right now. " +
           "It's what you'd pay to buy one more share or what you'd get if you sold one.",
    why:   "Price changes constantly based on what other people in the market are willing to pay. " +
           "It's the number that makes you money (when it goes up after you buy) or costs you money (when it drops).",
    how:   "Each market tick (about once a day in game time) updates every price. " +
           "Buying or selling doesn't change the price you see; it just changes how many shares you hold."
  },

  "change_percent": {
    label: "Change (%)",
    what:  "Change shows how much the price has moved over the recent window, as a percent. " +
           "+3.4 percent means the price is up 3.4 percent from where it started; -2.1 percent means it's down 2.1 percent.",
    why:   "It's a quick way to compare investments without having to do dollar math in your head. " +
           "A small green change percent on a Low risk holding is normal; a big green or red change percent on a High risk holding is also normal.",
    how:   "On Explore and Trade cards, this is the recent move shown next to the price. " +
           "On the Home tab graph, the trend badge shows the same idea applied to your whole portfolio over 30 days."
  }
};
