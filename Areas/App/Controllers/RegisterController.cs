using BlogApplication.Areas.App.ViewModels;
using BlogApplication.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApplication.Areas.App.Controllers
{
    [Area("App")]
    public class RegisterController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        protected readonly ILogger<RegisterController> _logger;
        public RegisterController(SignInManager<User> signInManager, UserManager<User> userManager, ILogger<RegisterController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;

        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(RegisterModel registrationData)
        {
            try
            {
                var user = CreateUser(registrationData);
                await ValidatePassword(user, registrationData.Password);
                if (!ModelState.IsValid)
                {
                    return View(registrationData);
                }
                var result = await _userManager.CreateAsync(user, registrationData.Password ?? "");
                if (!result.Succeeded)
                {
                    var message = result.Errors.FirstOrDefault()?.Description;
                    ViewData["Message"] = message ?? "Unknown error occured, Please try again later.";
                    _logger.LogInformation(message);
                    return View(registrationData);

                }
                return RedirectToAction("Index", "Login");
            }
            catch (Exception error)
            {
                ViewData["Message"] = "Unknown error occured, Please try again later.";
                _logger.LogError(error.Message);
                return View(registrationData);
            }

        }
        private async Task ValidatePassword(User user, string? password)
        {
            var passValidator = _userManager.PasswordValidators.First();
            if (!string.IsNullOrEmpty(password))
            {
                var passwordValidation = await passValidator.ValidateAsync(_userManager, user, password);

                if (!passwordValidation.Succeeded)
                {
                    var errorMsg = passwordValidation.Errors.FirstOrDefault()?.Description ?? "";
                    ModelState.AddModelError("Password", errorMsg);
                }

            }

        }
        private User CreateUser(RegisterModel registrationData)
        {
            return new User()
            {
                Email = registrationData.Email,
                GivenName = registrationData.GivenName ?? "",
                Surname = registrationData.Surname ?? "",
                UserName = registrationData.Email,
            };

        }
    }

}