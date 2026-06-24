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
| Doctors | `/api/v1/doctors/*` | UserService (+ расписание — stub в Gateway) |
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
| GET | `/api/v1/doctors/{id}/schedule?from=&days=` | Расписание (stub в Gateway) |

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
