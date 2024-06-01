using FluentValidation;

namespace s3backup.Configuration
{
    internal class ConfigurationBucket
    {
        public string? BucketName { get; set; }
        public string? AccessKeyId { get; set; }
        public string? SecretAccessKey { get; set; }
        public string? EndpointUrl { get; set; }
        public string? LocalStorage { get; set; }
        public string? RemoteStorage { get; set; }
        public string? Passphrase { get; set; }
    }

    internal class ConfigurationBucketValidator : AbstractValidator<ConfigurationBucket>
    {
        public ConfigurationBucketValidator()
        {
            RuleFor(opt => opt.BucketName).Cascade(CascadeMode.Stop)
                .NotEmpty();

            RuleFor(opt => opt.SecretAccessKey).Cascade(CascadeMode.Stop)
                .NotEmpty();

            RuleFor(opt => opt.AccessKeyId).Cascade(CascadeMode.Stop)
                .NotEmpty();

            RuleFor(opt => opt.EndpointUrl).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)");

            RuleFor(opt => opt.LocalStorage).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(x => Path.Exists(x));

            RuleFor(opt => opt.RemoteStorage).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches(@"^\/$|(^(?=\/)|^\.|^\.\.|^\~|^\~(?=\/))(\/(?=[^/\0])[^/\0]+)*\/$");

            RuleFor(opt => opt.Passphrase).Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Length(32);

        }
    }
}
