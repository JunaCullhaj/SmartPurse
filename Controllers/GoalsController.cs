using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Data;
using Expense_Tracker.Models;
using System.Threading.Tasks;

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
            return View(await _context.Goals.ToListAsync());
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
        public async Task<IActionResult> AddOrEdit(Goal goal)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (goal.Id == 0)
                {
                    if (user != null)
                        goal.UserId = user.Id;
                    _context.Add(goal);
                }
                else
                {
                    var existingGoal = await _context.Goals.FindAsync(goal.Id);
                    if (existingGoal == null)
                        return NotFound();

                    existingGoal.Title = goal.Title;
                    existingGoal.TargetAmount = goal.TargetAmount;
                    existingGoal.TargetDate = goal.TargetDate;

                    if (user != null)
                        existingGoal.UserId = user.Id;

                    _context.Update(existingGoal);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(goal);
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
