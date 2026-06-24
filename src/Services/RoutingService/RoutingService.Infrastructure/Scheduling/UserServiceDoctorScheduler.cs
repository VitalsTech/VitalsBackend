using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Vitals.AspNetCore.Authentication;

namespace RoutingService.Infrastructure.Scheduling;

public sealed class UserServiceDoctorScheduler : IDoctorScheduler
{
    private readonly HttpClient _http;
    private readonly UserServiceOptions _options;
    private readonly ILogger<UserServiceDoctorScheduler> _logger;
    private readonly StubDoctorScheduler _fallback;

    public UserServiceDoctorScheduler(
        HttpClient http,
        IOptions<UserServiceOptions> options,
        ILogger<UserServiceDoctorScheduler> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _fallback = new StubDoctorScheduler();

        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<DoctorSlotDto?> FindAvailableDoctorAsync(
        string specialty,
        int urgencyLevel,
        CancellationToken cancellationToken = default)
    {
        if (_options.UseStubSchedule)
            return await _fallback.FindAvailableDoctorAsync(specialty, urgencyLevel, cancellationToken).ConfigureAwait(false);

        try
        {
            var encodedSpecialty = Uri.EscapeDataString(specialty);
            var response = await _http.GetAsync(
                $"internal/doctors/find-available?specialty={encodedSpecialty}&urgencyLevel={urgencyLevel}",
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var doctor = await response.Content.ReadFromJsonAsync<UserServiceDoctorResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (doctor is null)
                return null;

            return new DoctorSlotDto
            {
                DoctorId = doctor.DoctorId,
                FullName = doctor.FullName,
                Specialty = doctor.Specialty,
                IsOnline = doctor.IsOnline,
                TodayLoad = doctor.TodayLoad,
                IsSenior = doctor.IsSenior
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UserService schedule lookup failed; using in-memory fallback");
            return await _fallback.FindAvailableDoctorAsync(specialty, urgencyLevel, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class UserServiceDoctorResponse
    {
        public Guid DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int TodayLoad { get; set; }
        public bool IsSenior { get; set; }
    }
}

public static class UserServiceDoctorSchedulerRegistration
{
    public static IServiceCollection AddDoctorScheduler(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UserServiceOptions>(configuration.GetSection(UserServiceOptions.SectionName));
        services.Configure<ServiceAuthOptions>(configuration.GetSection(ServiceAuthOptions.SectionName));

        services.AddHttpClient<IDoctorScheduler, UserServiceDoctorScheduler>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<UserServiceOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");

            var serviceAuth = sp.GetRequiredService<IOptions<ServiceAuthOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(serviceAuth.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Service-Key", serviceAuth.ApiKey);
                client.DefaultRequestHeaders.Add("X-Service-Name", "routing-service");
            }
        });

        return services;
    }
}
