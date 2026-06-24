# API Gateway

Единая точка входа для клиентов Vitals. Объединяет **AuthService**, **UserService**, **MedicalRecordService** и **AITriageService**.

Публичный префикс: **`/api/v1/`**

## Маршрутизация

| Публичный путь | Backend |
|----------------|---------|
| `POST /api/v1/auth/*` | AuthService (контроллер + DTO + FluentValidation) |
| `GET /api/v1/auth/esia/*` | AuthService (YARP) |
| `/api/v1/users/*` | UserService |
| `/api/v1/admin/*` | UserService |
| `/api/v1/medical-records/*` | MedicalRecordService |
| `/api/v1/triage/*` | AITriageService |
| `/api/v1/consultations/*` | ConsultationService |
| `/api/v1/consultations/hub/*` | ConsultationService (SignalR WebSocket) |
| `/api/v1/prescriptions/*` | PrescriptionService |
| `/api/v1/doctors` | 501 (заглушка) |

### Triage через Gateway

| Метод | Gateway | → Backend |
|-------|---------|-----------|
| POST | `/api/v1/triage/sessions` | `/api/triage/sessions` |
| POST | `/api/v1/triage/sessions/{id}/messages` | `/api/triage/sessions/{id}/messages` |
| GET | `/api/v1/triage/sessions/{id}` | `/api/triage/sessions/{id}` |

Требуется JWT (`Authorization: Bearer`).

## Middleware (порядок)

1. GlobalExceptionMiddleware — 500 + `errorId`
2. ResponseCompression — GZip / Brotli
3. RequestIdMiddleware — `X-Request-ID`
4. RequestLoggingMiddleware — метод, путь, статус, время, IP
5. JwtAuthenticationMiddleware — опциональный JWT
6. RateLimitingMiddleware — Redis sliding window
7. Authentication / Authorization
8. Controllers + YARP

## Rate limiting

| Политика | Лимит | Окно |
|----------|-------|------|
| Anonymous | 100 | 1 мин |
| Authenticated | 1000 | 1 мин |
| Login | 10 | 5 мин / IP |
| Register | 5 | 1 час / IP |

429 + `Retry-After`.

## Запуск (локально)

```bash
docker run -d -p 6379:6379 redis:7-alpine

# UserService :5195, AuthService :5200, MedicalRecordService :5210, AITriage :5220
cd src/Services/ApiGateway
dotnet run --project ApiGateway.API
```

Gateway: http://localhost:5080/swagger

Примеры:
- `POST http://localhost:5080/api/v1/auth/register`
- `POST http://localhost:5080/api/v1/triage/sessions` (с JWT)

## Docker

```bash
cd src/Services/ApiGateway
docker compose up -d
```

Порт **5000**. Требует запущенные backend-сервисы в сети `vitals-network`.

## JWT

Ключ берётся из `Jwt:JwksUrl` (AuthService `/internal/jwks`) или `Jwt:RsaPublicKeyPem`.
В Development при недоступном AuthService используется временный ключ (только для локальной отладки).
