using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Data;
using Expense_Tracker.Models;
using System.Threading.Tasks;

namespace Expense_Tracker.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Categories.ToListAsync());
        }

        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            if (id == 0)
                return View(new Category());
            else
                return View(await _context.Categories.FindAsync(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(Category category)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (category.CategoryId == 0)
                {
                    if (user != null)
                        category.UserId = user.Id;
                    _context.Add(category);
                }
                else
                {
                    var existingCategory = await _context.Categories.FindAsync(category.CategoryId);
                    if (existingCategory == null)
                        return NotFound();

                    existingCategory.Title = category.Title;
                    existingCategory.Icon = category.Icon;
                    existingCategory.Type = category.Type;

                    if (user != null)
                        existingCategory.UserId = user.Id;

                    _context.Update(existingCategory);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}