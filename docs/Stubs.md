# Заглушки и упрощённые реализации VitalsBackend

Сводный реестр всего, что **не является production-ready** или **ещё не подключено** в текущем монорепозитории. Обновляйте этот файл при замене заглушек на реальные интеграции.

## Сервисы без реализации

Следующие микросервисы из архитектуры Vitals **отсутствуют** в репозитории. В ApiGateway для части из них возвращается **501 Not Implemented**.

| Сервис / функция | Где видно | Поведение сейчас |
| --- | --- | --- |
| **Consultation Service** | `ApiGateway` → `/api/v1/consultations/*` | HTTP 501 |
| **Doctor search / schedule** | `ApiGateway` → `/api/v1/doctors/*` | HTTP 501 |
| **Prescription Service** | `ApiGateway` → `/api/v1/prescriptions/*` | HTTP 501 |
| **Notification Service** | — | Не реализован (ожидает события `auto_response_required`, уведомления пациентам) |
| **Integration Service** | — | Не реализован (лаборатории, скорая, внешние API) |
| **Emergency handler** | — | Не реализован (ожидает `emergency_required`) |

Файл заглушек Gateway: `src/Services/ApiGateway/ApiGateway.API/Controllers/V1/ServiceInfoControllers.cs`.

---

## ApiGateway

| Заглушка | Класс / файл | Описание | Как заменить |
| --- | --- | --- | --- |
| Неподключённые backend-ы | `ConsultationsController`, `DoctorsController`, `PrescriptionsController` | Любой запрос → **501** с текстом «not connected yet» | YARP-маршруты + запущенные сервисы |
| Routing Service не в Gateway | `appsettings.json` (YARP) | `/api/v1/routing/*` отсутствует; Routing — только internal HTTP :5230 | Добавить reverse proxy (если нужен публичный доступ) |
| Ephemeral JWT key (dev) | `JwtSigningKeyProvider` | Если JWKS недоступен в **Development**, генерируется временный RSA-ключ | Запускать AuthService; задать `Jwt:JwksUrl` или `Jwt:RsaPublicKeyPem` |
| Redis для rate limit | `RedisSlidingWindowRateLimitStore` | Работает только при доступном Redis (`localhost:6379`); без Redis — ошибка при старте | `docker run -p 6379:6379 redis:7-alpine` или docker-compose Gateway |

---

## AuthService

| Заглушка | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| SMS / Email для сброса пароля | `AuthenticationService` | Код сброса **только логируется**, не отправляется | `POST /api/auth/password/forgot` |
| ЕСИА (OAuth) | `EsiaOAuthService`, `EsiaController` | Код готов, но **`Esia:Enabled: false`** по умолчанию; без credentials → 503 | `Esia:ClientId`, `ClientSecret`, `RedirectUri`, `UserInfoEndpoint` |
| Ephemeral RSA signing key | `RsaKeyProvider` | Если `Jwt:RsaPrivateKeyPem` не задан, ключ **генерируется при каждом старте** | `Jwt:RsaPrivateKeyPem` в prod |

---

## UserService

| Заглушка / долг | Класс / файл | Описание |
| --- | --- | --- |
| Ключ шифрования | `EncryptionService` | Ключ/IV **дополняются паддингом** до нужной длины (`TODO` в коде); не KMS/HSM |
| AssignedBy при назначении роли | `RolesController` | `AssignedBy = null` (`TODO`: ID текущего администратора) |

Полноценной реализации UserService как «заглушки» нет — сервис рабочий, перечислены только незавершённые места.

---

## MedicalRecordService

| Заглушка | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Kafka publisher | `LoggingMedicalRecordEventPublisher` | События `medical-record.events` **только в лог**, без Confluent | `Kafka:Enabled: false` |
| JWT validation bypass (dev) | `Program.cs` | Если JWKS недоступен в Development — принимается любой Bearer-токен | `Jwt:JwksUrl` |

---

## AITriageService

| Заглушка / упрощение | Класс / файл | Описание | Конfig |
| --- | --- | --- | --- |
| Парсер симптомов (rule-based) | `RuleBasedSymptomParser` | Regex по русским формулировкам, **не NLP/ML** | — |
| NER | `StubNerService` | Нормализация + коды МКБ-10 **без внешней модели** | `MlServices:UseStubModels: true` |
| LLM триаж | `StubLlmTriageService` | Гипотезы, urgency, вопросы — **эвристики**, не LLM | `MlServices:UseStubModels: true` |
| Kafka publisher | `LoggingTriageEventPublisher` | Топик `triage.completed` **только в лог** | `Kafka:Enabled: false` |
| JWT validation bypass (dev) | `Program.cs` | См. Medical Record | `Jwt:JwksUrl` |

Эндпоинты реальных моделей (`MlServices:NerEndpoint`, `LlmEndpoint`) в DI **не подключены** — только stub-реализации.

---

## RoutingService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Движок маршрутизации (rule-based) | `RuleBasedRoutingEngine` | Правила if/else по гипотезам и urgency, **не ML и не plugin-система** | `RoutingEngine:AlgorithmVersion` |
| Расписание врачей | `StubDoctorScheduler` | 10 захардкоженных врачей, выбор по загрузке | — |
| Kafka consumer | `TriageCompletedConsumerHostedService` | При `Enabled: false` — ничего не слушает; при `true` — **consumer не реализован**, только warning | `Kafka:Enabled: false` |
| Kafka publisher | `LoggingRoutingEventPublisher` | `routing.decision`, `lab.order_required`, `emergency_required`, `auto_response_required` → **лог** | `Kafka:Enabled: false` |
| Redis для маршрутов | — | `Redis:Enabled: false`; состояние в **PostgreSQL** (`patient_routes`) | `Redis:Enabled: false` |
| JWT validation bypass (dev) | `Program.cs` | См. Medical Record | `Jwt:JwksUrl` |

Обработка triage без Kafka: **`POST /internal/routing/triage-completed`**.

---

## Межсервисные разрывы (интеграции-«заглушки»)

Связи, которые в архитектуре описаны через Kafka/HTTP, но **сейчас не замкнуты автоматически**:

| Поток | Ожидание | Сейчас |
| --- | --- | --- |
| AI Triage → Routing | `triage.completed` (Kafka) | Triage **логирует** событие; Routing принимает **ручной POST** на internal API |
| Routing → Consultation | `routing.decision` (Kafka) | Consultation Service **не существует** |
| Routing → Integration | `lab.order_required`, `emergency_required` | Integration Service **не существует** |
| Routing → Notification | `auto_response_required` | Notification Service **не существует** |
| Routing / Triage → Medical Record | HTTP контекст пациента | **Работает** (`GET internal/medical-records/patients/{id}/state`); при недоступности MR — routing/triage продолжают с `null` контекстом |

---

## Dev-only поведение (не для production)

| Где | Что происходит |
| --- | --- |
| AITriageService, RoutingService, MedicalRecordService | JWT не проверяется, если JWKS недоступен и `ASPNETCORE_ENVIRONMENT=Development` |
| ApiGateway | Временный RSA-ключ, если AuthService не запущен (Development) |
| AuthService | RSA-ключ для подписи JWT генерируется при старте, если PEM не задан |
| AuthService | Код восстановления пароля пишется в лог приложения |

---

## Быстрая проверка конфигурации

Во всех сервисах с Kafka типичные значения по умолчанию:

```json
"Kafka": { "Enabled": false }
```

AITriage:

```json
"MlServices": { "UseStubModels": true }
```

Routing:

```json
"Redis": { "Enabled": false }
```

Auth:

```json
"Esia": { "Enabled": false }
```

---

## Связанная документация

- [ApiGateway](ApiGateway.md) — маршруты и 501-заглушки
- [AuthService](AuthService.md) — ESIA, SMS/email
- [MedicalRecordService](MedicalRecordService.md) — Kafka
- [AITriageService](AITriageService.md) — NER/LLM/Kafka
- [RoutingService](RoutingService.md) — scheduler, Kafka, Redis
