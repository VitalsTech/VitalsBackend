using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vitals.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddVitalsKafka(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<KafkaConnectionOptions>? configure = null)
    {
        services.Configure<KafkaConnectionOptions>(configuration.GetSection(KafkaConnectionOptions.SectionName));
        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton<IKafkaMessageProducer, KafkaMessageProducer>();
        return services;
    }
}
