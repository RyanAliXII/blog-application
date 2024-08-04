using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
namespace BlogApplication.Services.EditorImageUrlExtractor{
    public class EditorJSDataBlock {
        [Required(ErrorMessage = "Id is required")]
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [Required(ErrorMessage = "Type is required")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public JsonObject? Data  { get; set;}
    }  
}