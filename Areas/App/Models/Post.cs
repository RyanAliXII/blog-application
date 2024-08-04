using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BlogApplication.Data;

namespace BlogApplication.Areas.App.Models{
    public class Post{
        public Guid Id {get; set; } = Guid.Empty;
        public string? Thumbnail { get; set; }
        public string Title { get; set;} = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid UserId { get; set; } = Guid.Empty;
        public virtual User? User { get; set; } 

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; }

    }
}