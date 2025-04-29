using System;

namespace Expense_Tracker.Models
{
    public class GoalDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal TargetAmount { get; set; }
        public DateTime TargetDate { get; set; }
    }
}
