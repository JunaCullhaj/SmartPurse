using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Data;
using Expense_Tracker.Models;

namespace Expense_Tracker.Controllers
{
    public class GoalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GoalsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Goals
        public async Task<IActionResult> Index()
        {
            var goals = await _context.Goals
                .ToListAsync();

            return View(goals);
        }

        // GET: Goals/AddOrEdit
        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            var user = await _userManager.GetUserAsync(User);

            if (id == 0)
                return View(new Goal());
            else
            {
                var goal = await _context.Goals
                    .Where(g => g.UserId == user.Id && g.Id == id)
                    .FirstOrDefaultAsync();

                if (goal == null)
                    return NotFound();

                return View(goal);
            }
        }

        // POST: Goals/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("Id,Title,TargetAmount,TargetDate")] Goal goal)
        {
            var user = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                if (goal.Id == 0)
                {
                    goal.UserId = user.Id;
                    _context.Add(goal);
                }
                else
                {
                    var existingGoal = await _context.Goals
                        .Where(g => g.UserId == user.Id && g.Id == goal.Id)
                        .FirstOrDefaultAsync();

                    if (existingGoal == null)
                        return NotFound();

                    existingGoal.Title = goal.Title;
                    existingGoal.TargetAmount = goal.TargetAmount;
                    existingGoal.TargetDate = goal.TargetDate;

                    _context.Update(existingGoal);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(goal);
        }

        // POST: Goals/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var goal = await _context.Goals
                .Where(g => g.UserId == user.Id && g.Id == id)
                .FirstOrDefaultAsync();

            if (goal == null)
                return NotFound();

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Goals/SuggestBasedOnTransactions
        [HttpPost]
        public async Task<JsonResult> SuggestBasedOnTransactions(decimal targetAmount, DateTime targetDate)
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.Today;
            var totalMonths = ((targetDate.Year - today.Year) * 12) + targetDate.Month - today.Month;

            if (totalMonths <= 0)
                totalMonths = 1; // Minimum 1 muaj për siguri

            var requiredMonthlySaving = targetAmount / totalMonths;

            // Marrim total income dhe expenses
            var income = await _context.Transactions
                .Where(t => t.UserId == user.Id && t.Amount > 0)
                .SumAsync(t => t.Amount);

            var expenses = await _context.Transactions
                .Where(t => t.UserId == user.Id && t.Amount < 0)
                .SumAsync(t => -t.Amount);

            var netAvailable = income - expenses;

            if (netAvailable >= requiredMonthlySaving)
            {
                return Json(new
                {
                    suggestion = $"You can save {Math.Round(requiredMonthlySaving, 2)} per month based on your current balance."
                });
            }
            else
            {
                // Gjej kategorinë ku shpenzon më shumë
                var biggestExpenseCategory = await _context.Transactions
                    .Where(t => t.UserId == user.Id && t.Amount < 0)
                    .GroupBy(t => t.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        Total = g.Sum(t => -t.Amount)
                    })
                    .OrderByDescending(g => g.Total)
                    .FirstOrDefaultAsync();

                if (biggestExpenseCategory != null)
                {
                    var categoryName = await _context.Categories
                        .Where(c => c.CategoryId == biggestExpenseCategory.CategoryId)
                        .Select(c => c.Title)
                        .FirstOrDefaultAsync();

                    return Json(new
                    {
                        suggestion = $"Reduce spending on \"{categoryName}\" to save an extra {Math.Round(requiredMonthlySaving - netAvailable, 2)} per month."
                    });
                }
                else
                {
                    return Json(new
                    {
                        suggestion = $"You need to adjust your expenses to save {Math.Round(requiredMonthlySaving, 2)} per month."
                    });
                }
            }
        }
    }
}
