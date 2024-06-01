using Amazon.S3;
using Amazon.S3.Model;

namespace s3backup
{
    internal static class Extensions
    {
        public static async Task<bool> CheckObjectExists(this IAmazonS3 client, string bucketName, string key)
        {

                try
                {
                    var response = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest()
                    {
                        BucketName = bucketName,
                        Key = key
                    });

                    return true;
                }

                catch (AmazonS3Exception ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return false;

                    //status wasn't not found, so throw the exception
                    throw;
                }
        }
        public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
        {
            foreach (T item in @this)
            {
                action(item);
            }
        }

        public static byte[] Slice(this byte[] bytes, int offset)
        {
            return bytes.Slice(offset, bytes.Length - offset);
        }

        public static byte[] Slice(this byte[] bytes, int offset, int count)
        {
            byte[] dataBytes = new byte[count];
            Array.Copy(bytes, offset, dataBytes, 0, count);

            return dataBytes;
        }

    }
}
