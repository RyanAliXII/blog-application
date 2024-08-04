
using BlogApplication.Areas.App.Models;
using BlogApplication.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogApplication.Repository{
    public class PostRepository(ApplicationDbContext dbContext) : EFRepository<Post>(dbContext), IPostRepository{
       protected readonly ApplicationDbContext _dbContext = dbContext;
       public IEnumerable<Post> GetByUserId(Guid userId){
            return [.. _dbContext.Post.Where(post=> post.UserId == userId).Include(post=>post.User).OrderByDescending(post=> post.CreatedAt)];
       }
       public async Task<Post?> GetByIdWithUser(Guid Id){
         return await _dbContext.Post.Where(post=> post.Id == Id).Include(post=>post.User).SingleOrDefaultAsync();
       }
       
         
    }
    public interface IPostRepository{
        public IEnumerable<Post> GetByUserId(Guid Id);
        public Task<Post?> GetByIdWithUser(Guid Id);
        
    }
   
}

