using BlogApplication.Areas.App.Models;
using Microsoft.EntityFrameworkCore;
namespace BlogApplication.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Post> Post { get; set; }
       
    }
}
