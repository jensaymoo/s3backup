using FluentValidation;

namespace s3backup.Configuration;

internal interface IConfigurationProvider
{
    public T GetConfiguration<T>(AbstractValidator<T>? validator = null);
}
