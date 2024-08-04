namespace  BlogApplication.Services.EditorImageUrlExtractor {
    public class EditorJSImageUrlExtractor : IEditorImageUrlExtractor{
        public IEnumerable<EditorJSDataBlock> ExtractWithBaseUrl(List<EditorJSDataBlock> blocks, string baseUrl){
            foreach (var block in blocks){
                var url = block.Data?["file"]?["url"]?.ToString() ?? "";
                if(block.Type == "image" && !string.IsNullOrEmpty(url) && url.StartsWith(baseUrl)){
                    yield return block;
                }
            }
           
        }       
    }
}