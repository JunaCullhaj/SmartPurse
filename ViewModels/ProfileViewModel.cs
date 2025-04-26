using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }  

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
