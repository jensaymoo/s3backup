using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Amazon.S3.Model;
using FluentValidation;
using s3backup.Actions;
using s3backup.Configuration;
using s3backup.Encryption;
using System.Security.Cryptography;
using System.Threading.Channels;
using IConfigurationProvider = s3backup.Configuration.IConfigurationProvider;

namespace s3backup
{
    internal class Restore : IProgram
    {
        private ConfigurationApp config;

        public Restore(IConfigurationProvider configProvider)
        {
            config = configProvider.GetConfiguration(new ConfigurationAppValidator());
        }

        public async Task Run()
        {
            //чекаем валидность настроек ведер
            config.Buckets!.ForEach(new ConfigurationBucketValidator().ValidateAndThrow);

            foreach (var configBucket in config.Buckets!)
            {
                Console.WriteLine($"Restore '{configBucket.BucketName}' to '{configBucket.LocalStorage}'.");

                var clientConfig = new AmazonS3Config()
                {
                    ServiceURL = configBucket.EndpointUrl,
                };

                var credConfig = new CredentialProfileOptions
                {
                    AccessKey = configBucket.AccessKeyId,
                    SecretKey = configBucket.SecretAccessKey,
                };

                var credFile = new SharedCredentialsFile();
                var credProfile = new CredentialProfile("credentials", credConfig);

                AWSCredentialsFactory.TryGetAWSCredentials(credProfile, credFile, out var credentials);
                using (var client = new AmazonS3Client(credentials, clientConfig))
                {

                    var abortTokenSource = new CancellationTokenSource();
                    await Task.Run(async () =>
                    {

                        try
                        {
                            var channel = CreateRestoreActionsChannel(client, configBucket, abortTokenSource, config.QueueCapacity);
                            await InvokeActions(client, channel, abortTokenSource, config.TaskLimit);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            await abortTokenSource.CancelAsync();
                        }
                    });
                }                
            }
            Console.WriteLine("Finished.");
        }


        private static Channel<ObjectAction> CreateRestoreActionsChannel(IAmazonS3 client, ConfigurationBucket configBucket, CancellationTokenSource cts, int capacityChannel)
        {
            if (capacityChannel <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacityChannel));

            CancellationToken ct = cts.Token;
            Channel<ObjectAction> channel = Channel.CreateBounded<ObjectAction>(capacityChannel);

            var readTask = Task.Run(async () =>
            {
                try
                {
                    var listRequest = new ListObjectsRequest
                    {
                        BucketName = configBucket.BucketName,
                        Prefix = configBucket.RemoteStorage,
                    };

                    do
                    {                        
                        ListObjectsResponse listResponse = await client.ListObjectsAsync(listRequest, ct);

                        foreach(var obj in listResponse.S3Objects)
                        {
                            var metadataResponse = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                            {
                                BucketName = configBucket.BucketName,
                                Key = obj.Key,
                            });

                            var hashObj = Cryptography.DecryptString(metadataResponse.Metadata["hash"], configBucket.Passphrase!);
                            var filePath = Path.Combine(configBucket.LocalStorage!, 
                                obj.Key.Replace(configBucket.RemoteStorage!, "")
                                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

                            if (File.Exists(filePath))
                            {
                                var hashFile = GetFileHash(filePath);
                                //сверяем хэш локального файла и обьекта в бакете
                                //если сходится то скипаем, иначе скачиваем новую версию
                                if (hashFile == hashObj)
                                {
                                    //скипаем
                                    await channel.Writer.WriteAsync(new SkipAction(obj.Key, configBucket));
                                }
                                else
                                {
                                    //скачиваем
                                    await channel.Writer.WriteAsync(new GetAction(filePath, obj.Key, configBucket));
                                }
                            }
                            else
                            {
                                //файл отсутсвует в локальном хранилище - скачиваем
                                await channel.Writer.WriteAsync(new GetAction(filePath, obj.Key, configBucket));
                            }
                        }

                        if (listResponse.IsTruncated)
                        {
                            listRequest.Marker = listResponse.NextMarker;
                        }
                        else
                        {
                            listRequest = null;
                        }

                    } while (listRequest != null);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    cts.Cancel();
                    throw;
                }
                finally
                {
                    channel.Writer.Complete();
                }
            });

            return channel;
        }
        private static async Task InvokeActions(IAmazonS3 client, Channel<ObjectAction> channel, CancellationTokenSource cts, int taskLimit)
        {
            if (taskLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(taskLimit));

            CancellationToken ct = cts.Token;

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < taskLimit; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using (var sha256 = SHA256.Create())
                    {
                        try
                        {
                            while (await channel.Reader.WaitToReadAsync(ct))
                            {
                                while (channel.Reader.TryRead(out var action))
                                {
                                    await action.Invoke(client);
                                }
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex)
                        {
                            await cts.CancelAsync();
                            throw;
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }
        private static string GetFileHash(string file)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(file))
                {
                    var hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }
    }
}
