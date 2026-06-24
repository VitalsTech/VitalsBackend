using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vitals.ObjectStorage;

public static class DependencyInjection
{
    public static IServiceCollection AddVitalsObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));
        var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>() ?? new ObjectStorageOptions();

        if (options.UseStub)
            services.AddSingleton<IObjectStorageProvider, StubObjectStorageProvider>();
        else
            services.AddSingleton<IObjectStorageProvider, S3ObjectStorageProvider>();

        return services;
    }
}
