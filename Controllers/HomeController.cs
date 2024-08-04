using BlogApplication.Models;
using BlogApplication.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BlogApplication.Controllers
{
    public class HomeController : Controller
    {
        protected readonly ILogger<HomeController> _logger;
        protected readonly PostRepository _postRepository;
        protected readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, PostRepository postRepository, IConfiguration config )
        {
            _logger = logger;
            _postRepository = postRepository;
            _config = config;
        }

        public IActionResult Index()
        {   var posts = _postRepository.GetAllOrderByLatest();
            var config = _config.GetSection("AWS");
            var serviceUrl = config["ServiceUrl"] ?? "";
            var bucket = config["Bucket"] ?? "";
            ViewData["S3Url"] =  $"{serviceUrl}/{bucket}";
            return View(posts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
