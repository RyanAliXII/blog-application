using System.Security.Claims;
using BlogApplication.Repository;
using Microsoft.AspNetCore.Authorization;

namespace BlogApplication.Policies.Requirements{
    public class PostOwnerHandler: AuthorizationHandler<PostOwnerRequirement>{
        protected readonly PostRepository _postRepository;
        protected readonly ILogger<PostOwnerHandler> _logger;
         public PostOwnerHandler(PostRepository postRepository, ILogger<PostOwnerHandler> logger)
        {
            _postRepository = postRepository;
            _logger = logger;
        }
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PostOwnerRequirement requirement)
        {
            var userIdString = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if(userIdString is null) {
                context.Fail();
                return;
            }
            
            var httpContext = (context.Resource as DefaultHttpContext)?.HttpContext;
            var routeData = httpContext?.GetRouteData();
            var postIdString = routeData?.Values?.GetValueOrDefault("id")?.ToString();
            
            if(postIdString is null) {
                context.Fail();
                return;
            }

            var isValidPostId = Guid.TryParse(postIdString, out Guid postId);

            if (!isValidPostId ) {
                context.Fail();
                return;
            }

            var isValidUserId = Guid.TryParse(userIdString, out Guid userId);

            if (!isValidUserId) {
                context.Fail();
                return;
            }
            
            var post = await _postRepository.GetByIdAsync(postId);
            if(post is null) {
                context.Fail();
                return;
            }

            if(post.UserId == userId){
                context.Succeed(requirement);
                return;
            }

            context.Fail();
            
        }
    }
}