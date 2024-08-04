using System.Configuration;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;


namespace BlogApplication.Services.FileStorage.S3
{
    public static class S3
    {
        public static IAmazonS3 CreateClient(IConfiguration config)
        {
            var awsConfig = config.GetSection("AWS");
            var awsCredential = new BasicAWSCredentials(awsConfig["AccessKeyId"], awsConfig["SecretAccessKey"]);
            var s3Config = new AmazonS3Config
            {
                ServiceURL = awsConfig["ServiceURL"],
                ForcePathStyle = true,

            };
            return new AmazonS3Client(awsCredential, s3Config);
        }
        public static void InitializeBucket(IAmazonS3 client, IConfiguration config)
        {
            CreateBucket(client, config);

        }
        private static async void CreateBucket(IAmazonS3 client, IConfiguration config)
        {
            var awsConfig = config.GetSection("AWS");
            var bucket = awsConfig["Bucket"] ?? throw new ConfigurationErrorsException("Missing Bucket in appsettings json");
            var isBucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(client, bucket);

            if (!isBucketExists)
            {
                await client.PutBucketAsync(new PutBucketRequest()
                {
                    BucketName = bucket,
                    BucketRegion = S3Region.APSoutheast1,
                    BucketRegionName = S3Region.APSoutheast1
                });
            }
            CreateBucketPolicy(client, bucket);
        }
        private static async void CreateBucketPolicy(IAmazonS3 client, string bucket)
        {
            var Policy = new Policy()
            {
                Version = "2012-10-17",
                Statement = [
                    new(){

                            Effect = "Allow",
                            Action = ["s3:GetObject"],
                            Principal = new Principal(){
                                AWS = ["*"]
                            },
                            Resource = [
                                $"arn:aws:s3:::{bucket}/contents/*",
                                $"arn:aws:s3:::{bucket}/temp/*"
                            ]

                    }
                ]
            };
            var policyJsonString = JsonSerializer.Serialize(Policy);
            await client.PutBucketPolicyAsync(new PutBucketPolicyRequest
            {
                BucketName = bucket,
                Policy = policyJsonString
            });
        }


    }

}