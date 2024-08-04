namespace BlogApplication.Services.FileStorage
{
    public interface IFileUploader
    {
        public IFileUploader SetContentType(string contentType);
        public IFileUploader SetFile(Stream file);
        public Task<UploadResponse> UploadAsync();



    }
    public record UploadResponse{
        public string Key = string.Empty;
    }
}
