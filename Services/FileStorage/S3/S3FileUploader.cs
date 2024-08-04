using BlogApplication.Services.FileStorage;
using Amazon.S3.Transfer;
using Amazon.S3;

public class S3FileUploader : IFileUploader
{
    private readonly IAmazonS3 _client;
    private string ContentType;
    private string Bucket;
    private Stream? File;
    private string Key;

    public S3FileUploader(IAmazonS3 client, Stream file, string key, string bucket )
    {
        _client = client;
        File = file;
        Bucket = bucket;
        ContentType = string.Empty;
        Key = key;
    }
    public S3FileUploader(IAmazonS3 client)
    {
        _client = client;
        File = null;
        Bucket = string.Empty;
        ContentType = string.Empty;
        Key = string.Empty;
    }
    public IFileUploader SetContentType(string contentType)
    {
        ContentType = contentType;
        return this;
    }
    public IFileUploader SetFile(Stream file)
    {

        File = file;
        return this;
    }
     public IFileUploader SetKey(string key)
    {

        Key = key;
        return this;
    }
    public async Task<UploadResponse> UploadAsync()
    {
        var transferUtility = new TransferUtility(_client);
        var transferOptions = new TransferUtilityUploadRequest()
        {  
            InputStream = File,
            BucketName = Bucket,
            ContentType = ContentType,
            Key = Key
        };
        await transferUtility.UploadAsync(transferOptions);
        return new UploadResponse{
            Key = Key,
        };
    }

}