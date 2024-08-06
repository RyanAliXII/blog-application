using System.Text.Json.Nodes;

namespace BlogApplication.Helpers{
public class EditorImageBlockSource{
        public JsonNode? Node;
        public string? Value;
        private JsonNode? _fileNode;
        private EditorImageBlockSource(){}
        public void UpdateSource(string? value){
            Value = value;
            if(_fileNode == null) return;
            _fileNode["url"] = JsonValue.Create(value);

        }
        public static EditorImageBlockSource GetImageSource(JsonObject? obj){
            var fileNode = obj?["file"];
            var urlNode = fileNode?["url"];
            var src = urlNode?.GetValue<string>();
            return new EditorImageBlockSource{
                Value = src,
                Node = urlNode,
                _fileNode = fileNode
            };
        }     
    }
    
}
