

using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Areas.App.ViewModels
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]

        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Given name is required")]
        public string? GivenName { get; set; }

        [Required(ErrorMessage = "Surname is required")]
        public string? Surname { get; set; }


    }
}