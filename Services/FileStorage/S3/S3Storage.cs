using Amazon.S3;
using Amazon.S3.Model;

namespace BlogApplication.Services.FileStorage.S3
{
    public class S3FileStorage : IFileStorage
    {
        private readonly IAmazonS3 _client;
        public S3FileStorage(IAmazonS3 client)
        {
            _client = client;
        }       
        public IFileUploader NewUploader()
        {
            return new S3FileUploader(_client);
        }
        public IFileUploader NewUploader(Stream fileStream, string Key,  string Bucket)
        {
            return new S3FileUploader(_client, fileStream, Key, Bucket);
        }
        public async Task CopyFile(string srcAndDestBucket, string srcKey, string destKey){
            await _client.CopyObjectAsync(new CopyObjectRequest{
                SourceBucket = srcAndDestBucket,
                DestinationBucket = srcAndDestBucket,
                SourceKey = srcKey,
                DestinationKey = destKey
            });
        }
        public async Task CopyFile(string srcBucket, string destBucket, string srcKey, string destKey){
            await _client.CopyObjectAsync(new CopyObjectRequest{
                SourceBucket = srcBucket,
                DestinationBucket = destBucket,
                SourceKey = srcKey,
                DestinationKey = destKey
            });
        }
        public async Task DeleteFile(string bucket, string key){
            await _client.DeleteObjectAsync(new DeleteObjectRequest{
                      BucketName = bucket,
                      Key = key
            });
            
        }

    }

}