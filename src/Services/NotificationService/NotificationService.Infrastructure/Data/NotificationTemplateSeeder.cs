using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Data;

public static class NotificationTemplateSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NotificationDbContext>>();

        var now = DateTime.UtcNow;
        var templates = new List<NotificationTemplate>
        {
            T("consultation.created.patient.push", "Push", "Новая консультация", "Врач {doctor_name} приглашает вас на консультацию.", now),
            T("consultation.created.patient.sms", "Sms", null, "Vitals: врач {doctor_name} ждёт вас на консультации.", now),
            T("consultation.created.doctor.push", "Push", "Новый пациент", "Пациент ожидает консультацию.", now),
            T("consultation.created.doctor.email", "Email", "Новая консультация Vitals", "У вас новая консультация с пациентом.", now),
            T("consultation.reminder.push", "Push", "Напоминание о консультации", "{doctor_name} ждёт вас через {minutes_remaining} минут.", now),
            T("consultation.reminder.sms", "Sms", null, "Vitals: консультация через {minutes_remaining} мин.", now),
            T("consultation.reminder.email", "Email", "Напоминание Vitals", "Консультация через {minutes_remaining} минут.", now),
            T("message.new.push", "Push", "Новое сообщение", "{preview}", now),
            T("message.new.email", "Email", "Новое сообщение Vitals", "{preview}", now),
            T("prescription.issued.push", "Push", "Новый рецепт", "Врач выписал рецепт на {medication_name}.", now),
            T("prescription.issued.email", "Email", "Рецепт Vitals", "Новый рецепт доступен в личном кабинете.", now),
            T("prescription.expiring.push", "Push", "Рецепт истекает", "Рецепт на {medication_name} истекает через {days_remaining} дней.", now),
            T("prescription.expiring.email", "Email", "Рецепт скоро истечёт", "Успейте забронировать {medication_name}.", now),
            T("lab.result_ready.push", "Push", "Анализы готовы", "Результаты анализа {test_name} доступны в приложении.", now),
            T("lab.result_ready.sms", "Sms", null, "Vitals: результаты анализа готовы. Зайдите в приложение.", now),
            T("lab.result_critical.push", "Push", "Критический результат", "Обнаружено критическое отклонение. Свяжитесь с врачом.", now),
            T("lab.result_critical.sms", "Sms", null, "Vitals: критический результат анализа. Зайдите в приложение.", now),
            T("lab.result_critical.voice", "Voice", null, "Vitals: критический результат анализа. Свяжитесь с врачом.", now),
            T("payment.failed.push", "Push", "Оплата не прошла", "Обновите данные карты для продолжения.", now),
            T("payment.failed.sms", "Sms", null, "Vitals: оплата не прошла. Обновите карту.", now),
            T("payment.failed.email", "Email", "Оплата Vitals", "Не удалось списать оплату.", now),
            T("payment.completed.push", "Push", "Оплата прошла", "Консультация оплачена. Спасибо!", now),
            T("payment.completed.email", "Email", "Оплата Vitals", "Оплата консультации успешно проведена.", now),
            T("consultation.completed.patient.push", "Push", "Консультация завершена", "Протокол доступен в личном кабинете.", now),
            T("consultation.completed.patient.email", "Email", "Консультация завершена", "Протокол консультации сохранён в медкарте.", now),
            T("consultation.completed.doctor.push", "Push", "Консультация завершена", "Протокол подписан и отправлен пациенту.", now),
            T("prescription.expired.push", "Push", "Рецепт истёк", "Рецепт на {medication_name} больше недействителен.", now),
            T("prescription.expired.email", "Email", "Рецепт истёк", "Срок действия рецепта истёк.", now),
            T("emergency.required.push", "Push", "Экстренная помощь", "Требуется срочная медицинская помощь.", now),
            T("emergency.required.sms", "Sms", null, "Vitals: экстренная ситуация. Свяжитесь с врачом.", now),
            T("emergency.required.voice", "Voice", null, "Vitals: экстренная ситуация. Немедленно свяжитесь с врачом.", now),
            T("triage.completed.push", "Push", "Триаж завершён", "Маршрутизация к врачу выполнена.", now),
            T("patient.mood.updated.push", "Push", "{title}", "{message}", now),
            T("patient.mood.updated.email", "Email", "{title}", "{message}\n\nОткрыть карточку: {deep_link}", now),
            T("patient.triage.completed.push", "Push", "{title}", "{message}", now),
            T("patient.triage.completed.email", "Email", "{title}", "{message}\n\nРекомендация: {recommendation}\n{deep_link}", now),
            T("system.maintenance.push", "Push", "Обслуживание", "Платформа будет недоступна {maintenance_time}.", now),
            T("system.maintenance.email", "Email", "Плановое обслуживание Vitals", "Сервис будет недоступен {maintenance_time}.", now),
            T("auto_response.push", "Push", "Ответ Vitals", "Мы подготовили ответ в базе знаний.", now),
            T("generic.system.push", "Push", "Vitals", "{message}", now)
        };

        var existingKeys = await db.Templates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => t.TemplateKey + "|" + t.Channel)
            .ToListAsync();

        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = templates
            .Where(t => !existing.Contains(t.TemplateKey + "|" + t.Channel))
            .ToList();

        if (missing.Count == 0)
            return;

        db.Templates.AddRange(missing);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} notification templates (missing keys)", missing.Count);
    }

    private static NotificationTemplate T(string key, string channel, string? subject, string body, DateTime now) => new()
    {
        Id = Guid.NewGuid(),
        TemplateKey = key,
        Language = "ru",
        Channel = channel,
        Subject = subject,
        Body = body,
        Version = 1,
        IsActive = true,
        UpdatedAt = now
    };
}
