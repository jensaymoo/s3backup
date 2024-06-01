using FluentValidation;
using Newtonsoft.Json;

namespace s3backup.Configuration
{
    internal class ConfigurationProviderJson : IConfigurationProvider
    {
        private string config;
        public ConfigurationProviderJson()
        {
            try
            {
                var asm_path = Directory.GetCurrentDirectory();
                config = File.ReadAllText(Path.Combine(asm_path, "config.json"));
            }
            catch
            {
                throw;
            }
        }

        public T GetConfiguration<T>(AbstractValidator<T>? validator = null)
        {
            var value = JsonConvert.DeserializeObject<T>(config)!;

            if (validator is not null)
                validator.ValidateAndThrow(value);

            return value;
        }
    }
}
