using Amazon.S3;
using Amazon.S3.Model;
using s3backup.Configuration;
using s3backup.Encryption;

namespace s3backup.Actions
{
    internal class GetAction(string File, string Key, ConfigurationBucket ConfigBucket) : ObjectAction
    {

        public override async Task Invoke(IAmazonS3 client)
        {
            var tempFile = Path.GetTempFileName();
            var request = new GetObjectRequest
            {
                BucketName = ConfigBucket.BucketName,
                Key = Key
            };

            var response = await client.GetObjectAsync(ConfigBucket.BucketName, Key);
            using (Stream responseStream = response.ResponseStream)
            using (FileStream fileStream = new FileStream(tempFile, FileMode.OpenOrCreate))
            {
                await responseStream.CopyToAsync(fileStream);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(File)!);
            await Cryptography.DecryptFileAsync(tempFile, File, ConfigBucket.Passphrase!);

            System.IO.File.Delete(tempFile);

            Console.WriteLine($"Object '{Key}' restored to '{ConfigBucket.LocalStorage}'.");
        }
    }
}
