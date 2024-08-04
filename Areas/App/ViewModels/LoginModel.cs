

using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Areas.App.ViewModels
{
    public class LoginModel
    {

        [EmailAddress(ErrorMessage = "Email format is invalid")]
        [Required(ErrorMessage = "Email format is required")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;


    }
}