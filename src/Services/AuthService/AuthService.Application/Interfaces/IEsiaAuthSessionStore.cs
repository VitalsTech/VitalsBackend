using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IEsiaAuthSessionStore
{
    void Save(EsiaAuthSession session);
    EsiaAuthSession? Take(string state);
}
