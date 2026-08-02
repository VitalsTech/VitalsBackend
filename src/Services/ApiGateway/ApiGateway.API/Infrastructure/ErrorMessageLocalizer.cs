using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiGateway.API.Infrastructure;

/// <summary>
/// Maps common English API errors to Russian for the Vitals RU product.
/// </summary>
public static partial class ErrorMessageLocalizer
{
    private static readonly (Regex Pattern, string Russian)[] Rules =
    [
        (SessionNotFound(), "Сессия не найдена."),
        (PrescriptionNotFound(), "Рецепт не найден."),
        (UserNotFound(), "Пользователь не найден."),
        (DoctorNotFound(), "Врач не найден."),
        (AccessDenied(), "Доступ запрещён."),
        (Unauthorized(), "Требуется авторизация."),
        (InvalidCredentials(), "Неверный телефон или пароль."),
        (InternalError(), "Внутренняя ошибка сервера."),
        (ValidationFailed(), "Проверьте правильность заполнения полей."),
        (OnlyDraftCanSign(), "Подписать можно только черновик рецепта."),
        (OnlyPrescribingDoctor(), "Подписать рецепт может только назначивший врач."),
        (SessionTerminal(), "Консультация уже завершена."),
        (NotParticipant(), "Вы не являетесь участником этой консультации."),
        (ProfileIdRequired(), "Укажите профиль."),
        (SlotNotFound(), "Слот расписания не найден."),
        (SlotInPast(), "Нельзя создавать слот в прошлом."),
        (EndBeforeStart(), "Время окончания должно быть позже начала."),
        (NoDoctorProfile(), "У пользователя нет профиля врача."),
        (DoctorProfileInactive(), "Профиль врача неактивен.")
    ];

    public static string? TryLocalizeJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;

            var localized = new Dictionary<string, object?>();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("detail", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("title", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("message", StringComparison.OrdinalIgnoreCase))
                {
                    var text = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.ToString();
                    localized[prop.Name] = Localize(text);
                }
                else if (prop.Name.Equals("errors", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var errors = new Dictionary<string, string[]>();
                    foreach (var field in prop.Value.EnumerateObject())
                    {
                        var messages = field.Value.ValueKind == JsonValueKind.Array
                            ? field.Value.EnumerateArray().Select(e => Localize(e.GetString())).ToArray()!
                            : [Localize(field.Value.GetString())];
                        errors[field.Name] = messages;
                    }
                    localized["errors"] = errors;
                }
                else
                {
                    localized[prop.Name] = prop.Value.Clone();
                }
            }

            return JsonSerializer.Serialize(localized);
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new { error = Localize(body) });
        }
    }

    public static string Localize(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Произошла ошибка. Попробуйте позже.";

        foreach (var (pattern, russian) in Rules)
        {
            if (pattern.IsMatch(message))
                return russian;
        }

        return message;
    }

    private static bool IsErrorStatus(HttpResponseMessage response) =>
        (int)response.StatusCode is >= 400 and <= 599;

    [GeneratedRegex("Session .* not found", RegexOptions.IgnoreCase)]
    private static partial Regex SessionNotFound();

    [GeneratedRegex("Prescription .* not found", RegexOptions.IgnoreCase)]
    private static partial Regex PrescriptionNotFound();

    [GeneratedRegex("User .* not found|Invalid user identity", RegexOptions.IgnoreCase)]
    private static partial Regex UserNotFound();

    [GeneratedRegex("Doctor .* not found", RegexOptions.IgnoreCase)]
    private static partial Regex DoctorNotFound();

    [GeneratedRegex("Access denied|Forbidden|Not authorized|UnauthorizedAccess", RegexOptions.IgnoreCase)]
    private static partial Regex AccessDenied();

    [GeneratedRegex("Unauthorized|Invalid token", RegexOptions.IgnoreCase)]
    private static partial Regex Unauthorized();

    [GeneratedRegex("Invalid phone|password", RegexOptions.IgnoreCase)]
    private static partial Regex InvalidCredentials();

    [GeneratedRegex("internal error|An internal error", RegexOptions.IgnoreCase)]
    private static partial Regex InternalError();

    [GeneratedRegex("validation|Validation", RegexOptions.IgnoreCase)]
    private static partial Regex ValidationFailed();

    [GeneratedRegex("Only draft prescriptions can be signed", RegexOptions.IgnoreCase)]
    private static partial Regex OnlyDraftCanSign();

    [GeneratedRegex("Only prescribing doctor", RegexOptions.IgnoreCase)]
    private static partial Regex OnlyPrescribingDoctor();

    [GeneratedRegex("terminal|already completed|already cancelled", RegexOptions.IgnoreCase)]
    private static partial Regex SessionTerminal();

    [GeneratedRegex("Not a session participant", RegexOptions.IgnoreCase)]
    private static partial Regex NotParticipant();

    [GeneratedRegex("ProfileId is required", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileIdRequired();

    [GeneratedRegex("Слот расписания не найден|Schedule slot", RegexOptions.IgnoreCase)]
    private static partial Regex SlotNotFound();

    [GeneratedRegex("Нельзя создавать слот|past", RegexOptions.IgnoreCase)]
    private static partial Regex SlotInPast();

    [GeneratedRegex("окончания должно|EndsAt", RegexOptions.IgnoreCase)]
    private static partial Regex EndBeforeStart();

    [GeneratedRegex("нет профиля врача|no doctor profile", RegexOptions.IgnoreCase)]
    private static partial Regex NoDoctorProfile();

    [GeneratedRegex("неактивен|inactive", RegexOptions.IgnoreCase)]
    private static partial Regex DoctorProfileInactive();
}
