using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Data;
using Expense_Tracker.Models;
using System.Threading.Tasks;
using System.Linq;

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

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var userGoals = await _context.Goals
                .Where(g => g.UserId == user.Id)
                .ToListAsync();

            return View(userGoals);
        }

        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View(new Goal());
            else
                return View(await _context.Goals.FindAsync(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([FromBody] GoalDTO goalDto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            if (goalDto == null)
                return BadRequest("Goal data is null.");

            Console.WriteLine($"Goal received: {goalDto.Title} {goalDto.TargetAmount} {goalDto.TargetDate}");

            if (goalDto.Id == 0)
            {
                var goal = new Goal
                {
                    Title = goalDto.Title,
                    TargetAmount = goalDto.TargetAmount,
                    TargetDate = goalDto.TargetDate,
                    UserId = user.Id
                };

                _context.Goals.Add(goal);
            }
            else
            {
                var existingGoal = await _context.Goals.FindAsync(goalDto.Id);
                if (existingGoal == null)
                    return NotFound();

                existingGoal.Title = goalDto.Title;
                existingGoal.TargetAmount = goalDto.TargetAmount;
                existingGoal.TargetDate = goalDto.TargetDate;
                existingGoal.UserId = user.Id;

                _context.Update(existingGoal);
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("? Goal saved successfully!");
            return Ok();
        }




        [HttpPost]
        public async Task<IActionResult> CalculateSuggestion([FromForm] decimal targetAmount, [FromForm] DateTime targetDate)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return BadRequest("User not found.");

                // Merr të ardhurat e muajit aktual
                var totalIncome = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == user.Id &&
                                t.Category.Type == "Income" &&
                                t.Date.Month == DateTime.Now.Month &&
                                t.Date.Year == DateTime.Now.Year)
                    .SumAsync(t => t.Amount);

                if (totalIncome <= 0)
                    totalIncome = 1;

                // Muajt deri në datë
                int monthsRemaining = ((targetDate.Year - DateTime.Now.Year) * 12) + (targetDate.Month - DateTime.Now.Month);
                if (monthsRemaining <= 0)
                    monthsRemaining = 1;

                var monthlySaving = targetAmount / monthsRemaining;
                if (monthlySaving > totalIncome)
                    monthlySaving = totalIncome;

                return Json(Math.Round(monthlySaving, 2));
            }
            catch (Exception ex)
            {
                return BadRequest("Server error: " + ex.Message);
            }
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var goal = await _context.Goals.FindAsync(id);
            if (goal != null)
            {
                _context.Goals.Remove(goal);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
