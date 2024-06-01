using Amazon.S3;
using s3backup.Configuration;

namespace s3backup.Actions
{
    internal class SkipAction(string Key, ConfigurationBucket ConfigBucket) : ObjectAction
    {
        public override Task Invoke(IAmazonS3 client)
        {
            Console.WriteLine($"Skiped, object '{Key}' is exist.");
            return Task.CompletedTask;
        }
    }
}
