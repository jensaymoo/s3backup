using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace s3backup.Encryption
{
    public static class Cryptography
    {
        private const int _iterations = 50000;
        private const string _salt = "98234jksd8ydsh8f7dysfh9h(YyaYASYF(AFASF*&^";

        public static async Task EncryptFileAsync(string inputFilePath, string outputFilePath, string password, CancellationToken token = default)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new ArgumentException("Input file does not exist!", nameof(inputFilePath));
            }

            byte[] saltBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] passBytes = Encoding.UTF8.GetBytes(password);

            using (var cipher = Aes.Create())
            {
                Rfc2898DeriveBytes _passwordBytes =
                    new Rfc2898DeriveBytes(passBytes, saltBytes, _iterations, HashAlgorithmName.SHA512);

                byte[] keyBytes = _passwordBytes.GetBytes(cipher.BlockSize / 8);

                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.ISO10126;

                cipher.GenerateIV();

                await using (var outputFileStream = new FileStream(outputFilePath, FileMode.Create))
                {
                    await outputFileStream.WriteAsync(cipher.IV, token);
                    using (var encryptor = cipher.CreateEncryptor(keyBytes, cipher.IV))
                    {
                        await using (var cryptoStream = new CryptoStream(outputFileStream, encryptor, CryptoStreamMode.Write))
                        {
                            await using var inputFileStream = new FileStream(inputFilePath, FileMode.Open);
                            await inputFileStream.CopyToAsync(cryptoStream, token);
                        }
                    }
                }

                cipher.Clear();
            }
        }

        public static string EncryptString(string value, string password)
        {
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);

            byte[] saltBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] passBytes = Encoding.UTF8.GetBytes(password);

            byte[] encrypted;

            using (var cipher = Aes.Create())
            {
                Rfc2898DeriveBytes _passwordBytes =
                    new Rfc2898DeriveBytes(passBytes, saltBytes, _iterations, HashAlgorithmName.SHA512);

                byte[] keyBytes = _passwordBytes.GetBytes(cipher.BlockSize / 8);

                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.ISO10126;

                cipher.GenerateIV();

                using (var encryptor = cipher.CreateEncryptor(keyBytes, cipher.IV))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        outputStream.Write(cipher.IV);

                        using (CryptoStream cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(valueBytes);
                            cryptoStream.FlushFinalBlock();
                        }

                        encrypted = outputStream.ToArray();
                    }
                }

                cipher.Clear();
            }
            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptString(string base64, string password)
        {
            byte[] saltBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] passBytes = Encoding.UTF8.GetBytes(password);

            var valueBytes = Convert.FromBase64String(base64);

            byte[] vectorBytes = valueBytes.Slice(0, 16);
            byte[] dataBytes = valueBytes.Slice(16);

            byte[] decrypted;
            int decryptedBytes;

            using (var cipher = Aes.Create())
            {
                Rfc2898DeriveBytes _passwordBytes =
                    new Rfc2898DeriveBytes(passBytes, saltBytes, _iterations, HashAlgorithmName.SHA512);

                byte[] keyBytes = _passwordBytes.GetBytes(cipher.BlockSize / 8);

                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.ISO10126;

                cipher.GenerateIV();

                using (var decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                {
                    using (MemoryStream from = new MemoryStream(dataBytes))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(from, decryptor, CryptoStreamMode.Read))
                        {
                            decrypted = new byte[dataBytes.Length];
                            decryptedBytes = cryptoStream.Read(decrypted);
                        }
                    }
                }

                cipher.Clear();
            }
            return Encoding.UTF8.GetString(decrypted, 0, decryptedBytes);
        }
        public static async Task DecryptFileAsync(string inputFilePath, string outputFilePath, string password, CancellationToken token = default)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new ArgumentException("Input file does not exist!", nameof(inputFilePath));
            }

            byte[] saltBytes = Encoding.ASCII.GetBytes(_salt);
            byte[] passBytes = Encoding.UTF8.GetBytes(password);

            using (var cipher = Aes.Create())
            {
                Rfc2898DeriveBytes _passwordBytes =
                    new Rfc2898DeriveBytes(passBytes, saltBytes, _iterations, HashAlgorithmName.SHA512);

                byte[] keyBytes = _passwordBytes.GetBytes(cipher.BlockSize / 8);

                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.ISO10126;

                cipher.GenerateIV();

                await using (var inputFileStream = new FileStream(inputFilePath, FileMode.Open))
                {
                    byte[] vectorBytes = new byte[cipher.BlockSize / 8];
                    int vectorBytesRead = await inputFileStream.ReadAsync(vectorBytes, token);

                    if (vectorBytesRead != vectorBytes.Length)
                    {
                        throw new ArgumentException("Failed to read initialization vector from input file!", nameof(inputFilePath));
                    }

                    using (var decryptor = cipher.CreateDecryptor(keyBytes, vectorBytes))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read))
                        {
                            await using var outFileStream = new FileStream(outputFilePath, FileMode.Create);
                            await cryptoStream.CopyToAsync(outFileStream, token);
                        }
                    }
                }

                cipher.Clear();
            }
        }
    }
}

