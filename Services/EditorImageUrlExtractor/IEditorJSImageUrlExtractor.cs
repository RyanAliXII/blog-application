namespace  BlogApplication.Services.EditorImageUrlExtractor {
    public interface IEditorImageUrlExtractor{
        public IEnumerable<EditorJSDataBlock> ExtractWithBaseUrl(List<EditorJSDataBlock> data, string baseUrl);       
    }
}