using FluentValidation;

namespace s3backup.Configuration
{
    internal class ConfigurationApp
    {
        public int TaskLimit { get; set; }
        public int QueueCapacity {  get; set; }
        public ConfigurationBucket[]? Buckets { get; set; }
    }

    internal class ConfigurationAppValidator : AbstractValidator<ConfigurationApp>
    {
        public ConfigurationAppValidator()
        {
            RuleFor(opt => opt.TaskLimit)
                .GreaterThanOrEqualTo(1);

            RuleFor(opt => opt.QueueCapacity)
                .GreaterThanOrEqualTo(32);

            RuleFor(opt => opt.Buckets)
                .NotEmpty();
        }
    }

}