using BlogApplication.Areas.App.ViewModels;
using BlogApplication.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace BlogApplication.Areas.App.Controllers
{
    [Area("App")]
    public class LoginController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginController> _logger;

        public LoginController(SignInManager<User> signInManager, ILogger<LoginController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Index(LoginModel creds, [FromQuery] string? ReturnUrl)
        {
            ReturnUrl = string.IsNullOrEmpty(ReturnUrl) ? "/app/dashboard" : ReturnUrl;
            if (!ModelState.IsValid)
            {
                ViewData["Message"] = "Invalid Credentials";
                return View(creds);
            }
            var result = await _signInManager.PasswordSignInAsync(creds.Email, creds.Password, true, false);
            if (!result.Succeeded)
            {
                ViewData["Message"] = "Invalid Credentials";
                creds.Password = "";
                return View(creds);
            }
            if(string.IsNullOrEmpty(ReturnUrl)){
                return RedirectToAction("Index", "Dashboard");
            }
            return Redirect(ReturnUrl);
            
        }
    }
}