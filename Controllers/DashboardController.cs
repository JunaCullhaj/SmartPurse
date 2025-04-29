using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {

        private readonly Expense_Tracker.Data.ApplicationDbContext _context;

        public DashboardController(Expense_Tracker.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            // Marrim të gjitha transaksionet me kategoritë e lidhura
            List<Transaction> AllTransactions = await _context.Transactions
                .Include(x => x.Category)
                .ToListAsync();

            // Total Income
            int TotalIncome = AllTransactions
                .Where(i => i.Category != null && i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("C0");

            // Total Expense
            int TotalExpense = AllTransactions
                .Where(i => i.Category != null && i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = TotalExpense.ToString("C0");

            // Balance = Income - Expense
            int Balance = TotalIncome - TotalExpense;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            // Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = AllTransactions
                .Where(i => i.Category != null && i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0"),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Spline Chart - Income vs Expense by Day

            // Income
            List<SplineChartData> IncomeSummary = AllTransactions
                .Where(i => i.Category != null && i.Category.Type == "Income")
                .GroupBy(j => j.Date.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.Key.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = AllTransactions
                .Where(i => i.Category != null && i.Category.Type == "Expense")
                .GroupBy(j => j.Date.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.Key.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            // Kombino të dhënat për 30 ditë e fundit
            DateTime startDate = DateTime.Today.AddDays(-29);
            string[] Last30Days = Enumerable.Range(0, 30)
                .Select(i => startDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineChartData = from day in Last30Days
                                      join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                      from income in dayIncomeJoined.DefaultIfEmpty()
                                      join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                      from expense in expenseJoined.DefaultIfEmpty()
                                      select new
                                      {
                                          day = day,
                                          income = income == null ? 0 : income.income,
                                          expense = expense == null ? 0 : expense.expense,
                                      };

            // Recent Transactions (5 më të fundit)
            ViewBag.RecentTransactions = AllTransactions
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToList();

            return View();
        }

    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;

    }
}
