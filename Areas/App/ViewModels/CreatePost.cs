using System.ComponentModel.DataAnnotations;
using BlogApplication.Services.EditorImageUrlExtractor;


namespace BlogApplication.Areas.App.ViewModels{
    public record CreatePost{
        
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;
        public string? Thumbnail { get; set; }
        [Required(ErrorMessage = "Content is required")]
        [MinLength(1, ErrorMessage ="Content is required.")]
        public List<EditorJSDataBlock> Content { get; set;} = [];

    }
}