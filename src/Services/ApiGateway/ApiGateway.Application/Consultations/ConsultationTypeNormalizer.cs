namespace ApiGateway.Application.Consultations;

/// <summary>
/// Canonical values match ConsultationService / Routing ConsultationFormat:
/// SyncChat, Video, Async, InPerson, HomeVisit.
/// Legacy gateway names AsyncChat / Audio are accepted as aliases.
/// </summary>
public static class ConsultationTypeNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "SyncChat";

        var key = value.Trim().ToLowerInvariant()
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

        return key switch
        {
            "syncchat" or "sync" or "chat" or "text" or "textchat"
                or "онлайн" or "чат" or "онлайнчат" or "1" => "SyncChat",

            "video" or "videocall" or "videaconsultation" or "видео"
                or "видеозвонок" or "аудио" or "audio" or "audiocall" or "voice" or "call" or "2" => "Video",

            "async" or "asyncchat" or "offline" or "message" or "asynchronous" or "3" => "Async",

            "inperson" or "offlinevisit" or "clinic" or "очный" or "очно" or "4" => "InPerson",

            "homevisit" or "home" or "надом" or "выезд" or "5" => "HomeVisit",

            _ => value.Trim()
        };
    }

    public static bool IsAllowed(string? value)
    {
        var normalized = Normalize(value);
        return normalized is "SyncChat" or "Video" or "Async" or "InPerson" or "HomeVisit";
    }
}
