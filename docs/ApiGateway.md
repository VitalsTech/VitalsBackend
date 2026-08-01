# API Gateway

Единая точка входа для клиентов Vitals. Все публичные эндпоинты объявлены в Gateway с **DTO** и **FluentValidation**; запросы проксируются во внутренние сервисы через типизированные HTTP-клиенты.

Публичный префикс: **`/api/v1/`**

## Контроллеры

| Контроллер | Путь | Backend |
|------------|------|---------|
| Auth | `/api/v1/auth/*` | AuthService |
| Users | `/api/v1/users/*` | UserService |
| Admin | `/api/v1/admin/*` | UserService |
| AdminRoles | `/api/v1/admin/roles/*` | UserService |
| Doctors | `/api/v1/doctors/*` | UserService (+ `me/calendar` — агрегация в Gateway) |
| MedicalRecords | `/api/v1/medical-records/*` | MedicalRecordService |
| Triage | `/api/v1/triage/*` | AITriageService |
| Consultations | `/api/v1/consultations/*` | ConsultationService |
| Prescriptions | `/api/v1/prescriptions/*` | PrescriptionService |
| Notifications | `/api/v1/notifications/*` | NotificationService |

### YARP (только прозрачный proxy)

| Путь | Назначение |
|------|------------|
| `/api/v1/auth/esia/*` | OAuth ESIA (редиректы AuthService) |
| `/api/v1/consultations/hub/*` | SignalR WebSocket |

## Middleware (порядок)

1. GlobalExceptionMiddleware — 500 + `errorId`
2. ResponseCompression — GZip / Brotli (>1 KB JSON)
3. RequestIdMiddleware — `X-Request-ID`
4. RequestLoggingMiddleware — метод, путь, статус, время, IP (без паролей/токенов)
5. JwtAuthenticationMiddleware — опциональный JWT в `HttpContext.User`
6. RateLimitingMiddleware — sliding window (Redis или in-memory fallback)
7. Authentication / Authorization — `[Authorize]` на защищённых контроллерах
8. Controllers + YARP (ESIA, SignalR)

## Rate limiting

| Политика | Лимит | Окно |
|----------|-------|------|
| Anonymous | 100 | 1 мин |
| Authenticated | 1000 | 1 мин |
| Login | 10 | 5 мин / IP |
| Register | 5 | 1 час / IP |

429 + `Retry-After`.

По умолчанию в dev: `RateLimiting:UseInMemoryFallback: true` (без Redis).  
Production: Redis (`RateLimiting:RedisConnectionString`).

## Валидация

FluentValidation на входящих DTO (auth, consultations, prescriptions, triage, medical records, notifications, admin).  
Ошибки → **400 Bad Request** до вызова backend.

## Doctors API

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/v1/doctors?specialization=&query=&page=&pageSize=` | Поиск врачей (агрегация через UserService admin search) |
| GET | `/api/v1/doctors/{id}` | Профиль врача |
| GET | `/api/v1/doctors/{id}/schedule?from=&days=` | Публичное расписание: только время и доступность слота |
| GET | `/api/v1/doctors/me/calendar?from=&days=` | Календарь врача с деталями занятости (роль Doctor) |
| PATCH | `/api/v1/doctors/me/profile` | Биография, специализация, учёная степень (роль Doctor) |
| POST | `/api/v1/doctors/me/schedule/slots` | Создать/обновить слот (роль Doctor) |
| DELETE | `/api/v1/doctors/me/schedule/slots/{slotId}` | Удалить слот (роль Doctor) |

### GET /api/v1/doctors/me/calendar

Единственный агрегирующий (не проксирующий) endpoint шлюза. Собирает ответ из четырёх сервисов:

| Источник | Запрос | Что даёт |
|----------|--------|----------|
| UserService | `GET internal/doctors/{id}/schedule` | сетку слотов вместе с данными брони |
| ConsultationService | `GET internal/consultations/doctors/sessions` | занятость слота и пациента |
| AITriageService | `GET internal/triage/sessions/{id}`, fallback `internal/triage/patients/{id}/sessions?limit=1` | результат триажа |
| MedicalRecordService | `GET internal/medical-records/patients/{id}/state` | анамнез |

Консультация связывается со слотом по `scheduledSlotId` из брони. Для сессий без брони
(созданы маршрутизацией или до появления бронирования) применяется запасное сопоставление по времени:
`scheduledAt ?? startedAt ?? createdAt` внутри `[startsAt, endsAt)`. Слот занят не более одной консультацией.

Незакрытые консультации возвращаются в `unscheduledConsultations` даже вне запрошенного окна,
но только если активность была за последние 14 дней — иначе брошенные чаты копились бы в каждом ответе.

Недоступный сервис обогащения не ломает ответ: соответствующее поле приходит `null`, сетка слотов и
`isBooked` возвращаются всегда.

```jsonc
{
  "doctorId": "…",
  "from": "2026-08-01T00:00:00Z",
  "to": "2026-08-08T00:00:00Z",
  "slots": [
    {
      "id": "…",
      "startsAt": "2026-08-01T09:00:00Z",
      "endsAt": "2026-08-01T10:00:00Z",
      "isOnline": true,
      "isAvailable": false,
      "isBooked": true,
      "status": "booked",              // booked | available | closed
      "consultation": {
        "sessionId": "…",
        "type": "Chat",
        "status": "Active",
        "isOpen": true,
        "urgencyLevel": 4,
        "scheduledAt": "2026-08-01T09:05:00Z",
        "unreadCount": 2,
        "videoRoomId": null,
        "patient": { "patientId": "…", "fullName": "Иванов Иван", "age": 42, "sex": "Male" },
        "triage": {
          "sessionId": "…",
          "urgencyLevel": 4,
          "urgency": "urgent",
          "urgencyLabel": "Срочно",
          "recommendedSpecialization": "Терапевт",
          "recommendation": "Записаться на приём в течение суток",
          "canBeRemote": true,
          "complaints": "Болит голова третий день",
          "symptoms": ["головная боль", "тошнота"],
          "hypotheses": [{ "condition": "Мигрень", "probability": 0.6 }],
          "emergencyWarning": false
        },
        "anamnesis": {
          "activeDiagnoses": ["I10 — Гипертензия"],
          "activeMedications": ["Энап, 10 мг"],
          "allergies": ["Пенициллин (высокая)"],
          "recentLabResults": ["Гемоглобин: 118 г/л"],
          "latestVital": "BloodPressure: 150/95 мм рт.ст.",
          "hasData": true
        }
      }
    }
  ],
  "unscheduledConsultations": []
}
```

## Запись на слот

`POST /api/v1/consultations/book` (роль Patient) — второй агрегирующий endpoint шлюза.

```jsonc
// запрос
{ "doctorId": "…", "slotId": "…", "consultationType": "SyncChat", "urgencyLevel": 3, "triageSessionId": null }

// 200
{
  "sessionId": "…",
  "doctorId": "…",
  "patientId": "…",
  "slotId": "…",
  "startsAt": "2026-08-05T09:00:00Z",
  "endsAt": "2026-08-05T10:00:00Z",
  "isOnline": true,
  "type": "SyncChat",
  "status": "Created",
  "slotLinked": true
}
```

Шаги внутри шлюза:

1. `POST internal/doctors/{doctorId}/schedule/slots/{slotId}/reserve` — один UPDATE с условиями
   «слот свободен, не забронирован, в будущем», поэтому параллельные записи на один слот не проходят.
2. `POST api/consultations` со `scheduledAt` из слота и `scheduledSlotId` — сессия всегда создаётся новая,
   существующий активный чат с этим врачом не переиспользуется.
3. `POST internal/doctors/schedule/slots/{slotId}/link-session` — привязка сессии к брони.

Если шаг 2 не удался, бронь снимается (`release`). Если не удался только шаг 3, запись остаётся
валидной: ответ приходит с `slotLinked: false`, а календарь сопоставит слот по времени.

| Код | Причина |
|-----|---------|
| 409 | слот уже занят, закрыт или в прошлом |
| 404 | врач или слот не найден |
| 502 | консультацию создать не удалось, бронь снята |

Забронированный слот врач не может изменить или удалить — `POST/DELETE me/schedule/slots` вернут 400.

## Запуск (локально)

```bash
cd src/Services/ApiGateway
dotnet run --project ApiGateway.API
```

Gateway: http://localhost:5080/swagger

Примеры:
- `POST http://localhost:5080/api/v1/auth/register`
- `GET http://localhost:5080/api/v1/doctors?specialization=Therapist`
- `POST http://localhost:5080/api/v1/triage/sessions` (с JWT)

## Docker

```bash
cd src/Services/ApiGateway
docker compose up -d
```

Порт **5000**. Redis для rate limit в Docker; backend-сервисы в сети `vitals-network`.

## JWT

Ключ из `Jwt:JwksUrl` (AuthService `/internal/jwks`) или `Jwt:RsaPublicKeyPem`.  
Development: временный ключ при недоступном AuthService.
