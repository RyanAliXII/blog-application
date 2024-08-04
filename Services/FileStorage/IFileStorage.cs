namespace BlogApplication.Services.FileStorage
{
    public interface IFileStorage
    {
         public IFileUploader NewUploader();
         public IFileUploader NewUploader(Stream fileStream, string Key, string Bucket);
         public  Task CopyFile(string srcAndDestBucket, string srcKey, string destKey);
         public  Task CopyFile(string srcBucket, string destBucket, string srcKey, string destKey);
         public Task DeleteFile(string bucket, string key);
    }
}