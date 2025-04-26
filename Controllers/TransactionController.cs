using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Data;
using Expense_Tracker.Models;
using System.Threading.Tasks;

namespace Expense_Tracker.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var transactions = _context.Transactions.Include(t => t.Category);
            return View(await transactions.ToListAsync());
        }

        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
                return View(await _context.Transactions.FindAsync(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit(Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (transaction.TransactionId == 0)
                {
                    if (user != null)
                        transaction.UserId = user.Id;
                    _context.Add(transaction);
                }
                else
                {
                    var existingTransaction = await _context.Transactions.FindAsync(transaction.TransactionId);
                    if (existingTransaction == null)
                        return NotFound();

                    existingTransaction.CategoryId = transaction.CategoryId;
                    existingTransaction.Amount = transaction.Amount;
                    existingTransaction.Date = transaction.Date;
                    existingTransaction.Note = transaction.Note;

                    if (user != null)
                        existingTransaction.UserId = user.Id;

                    _context.Update(existingTransaction);
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            PopulateCategories();
            return View(transaction);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public void PopulateCategories()
        {
            var categories = _context.Categories.ToList();
            Category defaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            categories.Insert(0, defaultCategory);
            ViewBag.Categories = categories;
        }
    }
}