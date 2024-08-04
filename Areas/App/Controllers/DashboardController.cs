using BlogApplication.Data;
using BlogApplication.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApplication.Areas.App.Controllers
{
    [Authorize]
    [Area("App")]
    public class DashboardController : Controller
    {
        protected readonly PostRepository _postRepository;
        protected readonly ILogger<DashboardController> _logger;
        protected readonly UserManager<User> _userManager;
          protected readonly IConfiguration _config;
        public DashboardController(PostRepository postRepository, ILogger<DashboardController> logger, UserManager<User> userManager,  IConfiguration config){
            _postRepository = postRepository;
            _logger = logger;
            _userManager = userManager;
            _config = config;
        }
        public async Task<IActionResult> Index()
        {   var config = _config.GetSection("AWS");
            
            var serviceUrl = config["ServiceUrl"] ?? "";
            var bucket = config["Bucket"] ?? "";
            ViewData["S3Url"] =  $"{serviceUrl}/{bucket}";
            var user = await _userManager.GetUserAsync(User) ?? throw new InvalidOperationException("User object is null");
            var posts = _postRepository.GetByUserId(user.Id);
            return View(posts ?? []);
        }
        
    }

}