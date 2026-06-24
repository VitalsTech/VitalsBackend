using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vitals.ESignature;

public static class DependencyInjection
{
    public static IServiceCollection AddVitalsESignature(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ESignatureOptions>(configuration.GetSection(ESignatureOptions.SectionName));

        var options = configuration.GetSection(ESignatureOptions.SectionName).Get<ESignatureOptions>() ?? new ESignatureOptions();
        if (options.UseStub)
        {
            services.AddSingleton<IESignatureProvider, StubESignatureProvider>();
            return services;
        }

        services.AddHttpClient<IESignatureProvider, HttpESignatureProvider>();
        return services;
    }
}
