using Amazon.S3;

namespace s3backup.Actions
{
    internal abstract class ObjectAction
    {
        public abstract Task Invoke(IAmazonS3 client);
    }
}
