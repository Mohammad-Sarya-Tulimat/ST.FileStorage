using Microsoft.Extensions.DependencyInjection;
using ST.FileStorage.Abstractions.Builders;
using System;
namespace ST.FileStorage.Abstractions
{
    public static class ServiceCollectionExtension
    {
        private static IFileService GetFileService(Action<FileServiceBuilder> build)
        {
            FileServiceBuilder builder = new FileServiceBuilder();
            build(builder);
            return builder.GetFileService();
        }
        public static IServiceCollection AddFileStorage(this IServiceCollection services, Action<FileServiceBuilder> build, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            if (serviceLifetime == ServiceLifetime.Singleton)
            {
                return services.AddSingleton(x => GetFileService(build));
            }
            else if (serviceLifetime == ServiceLifetime.Scoped)
            {
                return services.AddScoped(x => GetFileService(build));
            }
            else if (serviceLifetime == ServiceLifetime.Transient)
            {
                return services.AddTransient(x => GetFileService(build));
            }
            return services;
        }
    }
}
