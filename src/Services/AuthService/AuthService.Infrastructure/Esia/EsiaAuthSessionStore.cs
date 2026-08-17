using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Esia;

public sealed class EsiaAuthSessionStore : IEsiaAuthSessionStore
{
    private readonly IMemoryCache _cache;
    private readonly EsiaOptions _options;

    public EsiaAuthSessionStore(IMemoryCache cache, IOptions<EsiaOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public void Save(EsiaAuthSession session)
    {
        var ttl = TimeSpan.FromMinutes(Math.Clamp(_options.StateTtlMinutes, 2, 30));
        _cache.Set(Key(session.State), session, ttl);
    }

    public EsiaAuthSession? Take(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return null;

        var key = Key(state);
        if (!_cache.TryGetValue(key, out EsiaAuthSession? session))
            return null;

        _cache.Remove(key);
        return session;
    }

    private static string Key(string state) => $"esia:state:{state}";
}
