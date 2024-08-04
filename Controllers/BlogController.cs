using BlogApplication.Repository;
using Microsoft.AspNetCore.Mvc;

namespace BlogApplication.Controllers{
    public class BlogController: Controller{
        protected readonly PostRepository _postRepository;
        protected readonly ILogger<BlogController> _logger;

        public BlogController(PostRepository postRepository, ILogger<BlogController> logger){
            _postRepository = postRepository;
            _logger = logger;
        }
        [Route("/blogs/{id}/{title}")]
        public async Task<IActionResult> Index(Guid id, string? title){
            if(id == Guid.Empty){
                return NotFound();
            }

            var post = await _postRepository.GetByIdWithUser(id);
            if(post is null){
                return NotFound();
            }

            
            return View(post);
        }
    }
}