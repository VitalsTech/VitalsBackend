# Notification Service

Коммуникационный мост Vitals: доставка push, SMS, email и голосовых уведомлений по событиям платформы.

## Компоненты (текущая версия)

| Компонент | Реализация |
| --- | --- |
| Kafka consumer | **Заглушка** — события через `POST /internal/notifications/events` |
| Push (APNS/FCM) | **Заглушка** (`PushChannelDispatcher`) |
| SMS / Email / Voice | **Заглушки** через Integration Service (`UseStub: true`) |
| Шаблоны | PostgreSQL + seed ~25 шаблонов (ru) |
| Redis (кэш шаблонов/настроек) | **Не подключён** (`Redis:Enabled: false`) |
| Настройки пользователя | Локальная таблица + дефолты (User Service — будущая интеграция) |
| Дедупликация | Таблица `processed_events` |
| Повторы | `NotificationRetryHostedService` (экспоненциальная задержка) |
| Аудит | `notification_delivery_logs` |

## Поддерживаемые события

| eventType | Каналы |
| --- | --- |
| `consultation.created` | push, sms, email (фильтр по `templateData.recipient_role`: `patient` / `doctor`) |
| `consultation.reminder` | push, sms, email |
| `message.new` | push, email |
| `prescription.issued` / `prescription.created` | push, email |
| `prescription.expiring_soon` | push, email |
| `lab.result_ready` | push, sms |
| `lab.result_critical` | push, sms, voice |
| `payment.failed` | push, sms, email |
| `system.maintenance` | push, email |
| `auto_response_required` | push |
| `patient.mood.updated` | push, email → врачи (категория `mood`) |
| `patient.triage.completed` | push, email → врачи (категория `triage`) |

Получатели mood/triage: активные doctor access-grants ∪ врач последней консультации (MedicalRecordService). Идемпотентность: `eventId` = MD5(medicalEventId + doctorId + eventType).

Категории preferences: `consultations`, `messages`, `labs`, `prescriptions`, `payments`, `marketing`, `system`, `mood`, `triage` (по умолчанию pushEnabled=true).

## API (JWT)

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/api/notifications/push-tokens` | Регистрация push-токена устройства |
| GET | `/api/notifications/preferences` | Настройки уведомлений |
| PUT | `/api/notifications/preferences` | Обновить категорию (кроме `system`) |
| GET | `/api/notifications/history` | История отправок пользователя |

Internal (без JWT):

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/internal/notifications/events` | Обработать событие Kafka |
| POST | `/internal/notifications/send` | Ручная отправка |
| GET | `/internal/notifications/{deliveryId}/status` | Статус доставки |
| GET | `/internal/notifications/users/{userId}/history` | История по userId |
| GET | `/internal/notifications/stats` | Метрики за последний час |
| GET | `/internal/notifications/templates` | Список шаблонов |
| PUT | `/internal/notifications/templates/{key}/{channel}` | Новая версия шаблона |

## Пример

```json
POST /internal/notifications/events
{
  "eventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "eventType": "prescription.expiring_soon",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "priority": "Low",
  "templateData": {
    "medication_name": "Амоксициллин",
    "days_remaining": "3"
  }
}
```

Для `consultation.created` укажите роль получателя:

```json
{
  "eventType": "consultation.created",
  "userId": "<patientId>",
  "secondaryUserId": "<doctorId>",
  "templateData": {
    "recipient_role": "patient",
    "doctor_name": "Иванова И.И."
  }
}
```

## Запуск

```bash
cd src/Services/NotificationService
docker compose up -d postgres-notification
dotnet run --project NotificationService.API
```

Swagger: [http://localhost:5260/swagger](http://localhost:5260/swagger)  
PostgreSQL: **5439**  
Gateway: `/api/v1/notifications/*` (публичные JWT-эндпоинты)

## Связанные заглушки

См. [Stubs.md](Stubs.md) — раздел NotificationService.
