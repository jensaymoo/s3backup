using Amazon.S3;
using Amazon.S3.Model;
using s3backup.Configuration;
using s3backup.Encryption;

namespace s3backup.Actions
{
    internal class PutAction(string File, string Hash, string Key, ConfigurationBucket ConfigBucket) : ObjectAction
    {
        public override async Task Invoke(IAmazonS3 client)
        {
            var tempFile = Path.GetTempFileName();
            await Cryptography.EncryptFileAsync(File, tempFile, ConfigBucket.Passphrase!);

            var request = new PutObjectRequest
            {
                BucketName = ConfigBucket.BucketName,
                FilePath = tempFile,
                Key = Key,
            };

            //шифруем хэш и добавляем его в метаданные обьекта
            request.Metadata.Add("hash", Cryptography.EncryptString(Hash, ConfigBucket.Passphrase!));

            var response = await client.PutObjectAsync(request);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                Console.WriteLine($"Successfully uploaded '{request.Key}' to '{request.BucketName}'.");
            else
                Console.WriteLine($"Could not upload '{request.Key}' to '{request.BucketName}'.");

            System.IO.File.Delete(tempFile);
        }
    }
}
