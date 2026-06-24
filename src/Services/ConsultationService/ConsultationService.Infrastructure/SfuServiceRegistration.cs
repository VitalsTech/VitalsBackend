using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using ConsultationService.Infrastructure.Video;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConsultationService.Infrastructure;

public static class SfuServiceRegistration
{
    public static IServiceCollection AddSfuSignaling(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SfuOptions>(configuration.GetSection(SfuOptions.SectionName));
        var sfu = configuration.GetSection(SfuOptions.SectionName).Get<SfuOptions>() ?? new SfuOptions();

        if (sfu.UseStub)
        {
            services.AddSingleton<ISfuSignalingService, StubSfuSignalingService>();
            return services;
        }

        if (string.Equals(sfu.Provider, "Http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ISfuSignalingService, HttpSfuSignalingService>();
            return services;
        }

        services.AddHttpClient<ISfuSignalingService, LiveKitSfuSignalingService>();
        return services;
    }
}
