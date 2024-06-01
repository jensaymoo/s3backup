using Autofac;
using s3backup.Configuration;
using Serilog;

namespace s3backup
{
    internal class MainEntry
    {
        static async Task Main(string[] args)
        {
            var builder = new ContainerBuilder();
            ILifetimeScope scope;
            IProgram instance;

            try
            {
                builder.RegisterType<ConfigurationProviderJson>()
                    .As<IConfigurationProvider>()
                    .InstancePerLifetimeScope();

                //проверяем режим работы 
                if (args.Any(x => x == "restore")) 
                {
                    builder.RegisterType<Restore>()
                        .As<IProgram>()
                        .InstancePerLifetimeScope();
                } 
                else
                {
                    builder.RegisterType<Backup>()
                        .As<IProgram>()
                        .InstancePerLifetimeScope();
                }


                scope = builder.Build().BeginLifetimeScope();
                instance = scope.Resolve<IProgram>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            await instance.Run();
        }
    }
}
