using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Data;
using Expense_Tracker.Models;
using System.Threading.Tasks;
using System.Linq;

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
            var user = await _userManager.GetUserAsync(User);

            var transactions = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == user.Id);

            return View(await transactions.ToListAsync());
        }

        public async Task<IActionResult> AddOrEdit(int id = 0)
        {
            await PopulateCategories();

            if (id == 0)
                return View(new Transaction());
            else
            {
                var transaction = await _context.Transactions.FindAsync(id);
                var user = await _userManager.GetUserAsync(User);

                if (transaction == null || transaction.UserId != user.Id)
                    return NotFound();

                return View(transaction);
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]  // Sepse Ajax nuk dërgon token
        public async Task<IActionResult> AddOrEdit([FromBody] Transaction transaction)
        {
            var user = await _userManager.GetUserAsync(User);

            if (transaction == null)
                return BadRequest();

            if (transaction.TransactionId == 0)
            {
                if (user != null)
                    transaction.UserId = user.Id;

                _context.Add(transaction);
            }
            else
            {
                var existingTransaction = await _context.Transactions.FindAsync(transaction.TransactionId);

                if (existingTransaction == null || existingTransaction.UserId != user.Id)
                    return NotFound();

                existingTransaction.CategoryId = transaction.CategoryId;
                existingTransaction.Amount = transaction.Amount;
                existingTransaction.Date = transaction.Date;
                existingTransaction.Note = transaction.Note;

                _context.Update(existingTransaction);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions.FindAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (transaction == null || transaction.UserId != user.Id)
                return NotFound();

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public async Task PopulateCategories()
        {
            var user = await _userManager.GetUserAsync(User);

            var categories = await _context.Categories
                .Where(c => c.UserId == user.Id)
                .Select(c => new Category
                {
                    CategoryId = c.CategoryId,
                    Title = c.Icon + " " + c.Title,
                    Icon = c.Icon,
                    Type = c.Type,
                    UserId = c.UserId
                })
                .ToListAsync();

            Category defaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            categories.Insert(0, defaultCategory);

            ViewBag.Categories = categories;
        }
    }
}
