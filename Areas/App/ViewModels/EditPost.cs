using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BlogApplication.Services.EditorImageUrlExtractor;

namespace BlogApplication.Areas.App.ViewModels{
    public record EditPost{

        
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [Required]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }
        [Required(ErrorMessage = "Content is required")]
        [MinLength(1, ErrorMessage ="Content is required.")]
        [JsonPropertyName("content")]
        public List<EditorJSDataBlock> Content { get; set;} = [];

    }
}