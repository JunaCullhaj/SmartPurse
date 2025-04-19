using Expense_Tracker.Data;
using Expense_Tracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Expense_Tracker.Controllers
{
    [Authorize]
    public class GoalsController : Controller
    {
        private readonly Expense_Tracker.Data.ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GoalsController(Expense_Tracker.Data.ApplicationDbContext context, UserManager<ApplicationUser> userManager)

        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var goals = await _context.Goals.Where(g => g.UserId == user.Id).ToListAsync();
            return View(goals);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Goal goal)
        {
            if (!ModelState.IsValid)
                return View(goal);

            var user = await _userManager.GetUserAsync(User);
            goal.UserId = user.Id;

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Suggest(int id)
        {
            var goal = await _context.Goals.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);

            var income = await _context.Transactions
                .Where(t => t.UserId == user.Id && t.Amount > 0)
                .SumAsync(t => t.Amount);

            var expenses = await _context.Transactions
                .Where(t => t.UserId == user.Id && t.Amount < 0)
                .SumAsync(t => -t.Amount);

            var net = income - expenses;
            var months = ((goal.TargetDate - DateTime.Today).Days / 30.0);
            var suggestion = goal.TargetAmount / (decimal)Math.Max(months, 1);

            ViewBag.Suggestion = Math.Round(suggestion, 2);
            ViewBag.NetAvailable = net;

            return View(goal);
        }
    }
}
