# Заглушки и упрощённые реализации VitalsBackend

Сводный реестр всего, что **не является production-ready** или **ещё не подключено** в текущем монорепозитории. Обновляйте этот файл при замене заглушек на реальные интеграции.

## Сервисы без реализации

Следующие микросервисы из архитектуры Vitals **отсутствуют** в репозитории. В ApiGateway для части из них возвращается **501 Not Implemented**.

| Сервис / функция | Где видно | Поведение сейчас |
| --- | --- | --- |
| **Doctor search / schedule** | `DoctorsController` → UserService | Поиск через UserService; расписание из `DoctorScheduleSlots` |
| **Integration Service** | `IntegrationService` (:5270) | HTTP + Kafka; payment/voice/EGISZ/storage endpoints; SMS/email/push/pharmacy/lab — **лог-stub** |
| **Payment Service** | `PaymentService` (:5280) | Каркас; gateway через Integration |
| **Analytics Service** | `AnalyticsService` (:5290) | Kafka metrics + dashboard API |
| **Quality Service** | `QualityService` (:5295) | Kafka quality scores + metrics API |
| **Emergency handler** | `EmergencyRequiredConsumerHostedService` | Consumer `emergency_required` → HTTP dispatch stub |

Файл заглушек Gateway: `src/Services/ApiGateway/ApiGateway.API/Controllers/V1/ServiceInfoControllers.cs`.

---

## ApiGateway

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Расписание врачей | `DoctorsController.GetSchedule` | Прокси в UserService (`DoctorScheduleSlots`) | — |
| Rate limit без Redis (dev) | `InMemorySlidingWindowRateLimitStore` | In-memory fallback | `RateLimiting:UseInMemoryFallback: true` |
| ESIA OAuth | YARP `auth-esia` | Прозрачный proxy (без DTO Gateway) | — |
| SignalR чат | YARP `consultation-hub` | WebSocket proxy | — |
| Routing Service | — | Internal-only :5230 | — |
| Ephemeral JWT key (dev) | `JwtSigningKeyProvider` | Временный RSA при недоступном JWKS | `Jwt:JwksUrl` |

Все остальные публичные API — **контроллеры Gateway** с DTO + FluentValidation + `IBackendForwarder`.

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
| Kafka publisher | `KafkaMedicalRecordEventPublisher` | Confluent producer; при `Enabled: false` — no-op | `Kafka:Enabled: false` |
| JWT validation | `AddVitalsAuthentication` | JWKS / service key; dev bypass только через `appsettings.Development.json` | `Jwt:JwksUrl`, `ServiceAuth:ApiKey` |

---

## AITriageService

| Заглушка / упрощение | Класс / файл | Описание | Конfig |
| --- | --- | --- | --- |
| Парсер симптомов (rule-based) | `RuleBasedSymptomParser` | Regex по русским формулировкам, **не NLP/ML** | — |
| NER | `HttpNerService` / `StubNerService` | HTTP к `MlServices:NerEndpoint`; fallback на rule-based stub | `MlServices:UseStubModels: true` |
| LLM триаж | `HttpLlmTriageService` / `StubLlmTriageService` | HTTP к `MlServices:LlmEndpoint`; fallback на эвристики | `MlServices:UseStubModels: true` |
| Kafka publisher | `KafkaTriageEventPublisher` | Confluent producer; при `Enabled: false` — no-op | `Kafka:Enabled: false` |
| JWT validation | `AddVitalsAuthentication` | См. Medical Record | `Jwt:JwksUrl` |

Эндпоинты реальных моделей (`MlServices:NerEndpoint`, `LlmEndpoint`) в DI **не подключены** — только stub-реализации.

---

## RoutingService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Движок маршрутизации (rule-based) | `RuleBasedRoutingEngine` | Правила if/else по гипотезам и urgency, **не ML и не plugin-система** | `RoutingEngine:AlgorithmVersion` |
| Расписание врачей | `UserServiceDoctorScheduler` / `StubDoctorScheduler` | HTTP к UserService `DoctorScheduleSlots`; fallback — 10 hardcoded doctors | `UserService:UseStubSchedule: true` |
| Kafka consumer | `TriageCompletedConsumerHostedService` | Confluent consumer `triage.completed` | `Kafka:Enabled: false` |
| Kafka publisher | `KafkaRoutingEventPublisher` | Confluent producer | `Kafka:Enabled: false` |
| Redis для маршрутов | — | `Redis:Enabled: false`; состояние в **PostgreSQL** (`patient_routes`) | `Redis:Enabled: false` |
| JWT validation | `AddVitalsAuthentication` | См. Medical Record | `Jwt:JwksUrl` |

Обработка triage без Kafka: **`POST /internal/routing/triage-completed`**.

---

## ConsultationService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Kafka consumer | `RoutingDecisionConsumerHostedService` | Confluent consumer `routing.decision` | `Kafka:Enabled: false` |
| Kafka publisher | `KafkaConsultationEventPublisher` | Confluent producer | `Kafka:Enabled: false` |
| SFU / WebRTC | `LiveKitSfuSignalingService`, `HttpSfuSignalingService`, `StubSfuSignalingService` | По умолчанию **P2P WebRTC** (`mode: p2p`, STUN + SignalR). LiveKit/HTTP при `Sfu:UseStub: false` | `Sfu:UseStub: true` |
| Redis hot state | `InMemorySessionStateStore` | Статус/unread **логируются**, персистентность в PostgreSQL | `Redis:Enabled: false` |
| Напоминания push/SMS | — | Только `SessionTimeoutHostedService` (expire scan) | — |
| JWT validation | `AddVitalsAuthentication` | См. Medical Record | `Jwt:JwksUrl` |

---

## PrescriptionService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Движок проверок | `RuleBasedPrescriptionValidationEngine` | Hardcoded аллергии/взаимодействия/беременность | — |
| Права врача | `StubUserPermissionClient` | Блок ATC N05/N06, остальное разрешено | — |
| Электронная подпись | `Vitals.ESignature` (`HttpESignatureProvider` / stub) | КЭП через HTTP gateway; stub Base64 по умолчанию | `ESignature:UseStub: true` |
| Integration / аптека | `StubPharmacyIntegrationClient` | Fake order id | `IntegrationService:UseStub: true` |
| Kafka | `KafkaPrescriptionEventPublisher` | Confluent producer; при `Enabled: false` — no-op | `Kafka:Enabled: false` |
| Льготы / ЕГИСЗ | `IntegrationEgiszClient` | Проверка через Integration `egisz/preferential/check`; stub при `UseStub: true` | `IntegrationService:UseStub: true` |
| JWT validation | `AddVitalsAuthentication` | См. Medical Record | `Jwt:JwksUrl` |

---

## NotificationService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Kafka consumer | `KafkaNotificationConsumerHostedService` | Confluent consumer; fallback — **POST /internal/notifications/events** | `Kafka:Enabled: false` |
| Push (APNS/FCM) | `PushChannelDispatcher` | При `IntegrationService:UseStub: false` → HTTP Integration; иначе лог | `IntegrationService:UseStub: true` |
| SMS / Email / Voice | `SmsChannelDispatcher`, `EmailChannelDispatcher`, `VoiceChannelDispatcher` | SMS/Email/Voice → Integration HTTP или лог-stub | `IntegrationService:UseStub: true` |
| DLQ | `DeadLetterNotification`, `NotificationOrchestrator` | После max retries → `dead_letter_notifications`; API `GET /internal/notifications/dlq` | `Notification:MaxRetryAttempts` |
| Полный набор events | `EventChannelRouter` | `consultation.completed`, `prescription.expired`, `emergency.required`, `payment.completed`, … | — |
| Redis (шаблоны, dedup, prefs) | — | Всё в **PostgreSQL** | `Redis:Enabled: false` |
| Настройки из User Service | `UserPreferenceService` | Локальная таблица + дефолты | — |
| JWT validation | `AddVitalsAuthentication` | См. Medical Record | `Jwt:JwksUrl` |

Обработка без Kafka: **`POST /internal/notifications/events`**.

---

## IntegrationService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| SMS / Email / Push | `IntegrationDispatchService` | HTTP API готов; провайдеры — **лог-stub** | — |
| Payment gateway | `ProcessPaymentAsync` | `POST /internal/integration/payments/process` — stub (auto-complete) | — |
| Voice calls | `SendVoiceCallAsync` | `POST /internal/integration/voice/call` — лог-stub | — |
| ЕГИСЗ льготы | `CheckPreferentialEligibilityAsync` | `POST /internal/integration/egisz/preferential/check` — stub | — |
| Object storage | `UploadObjectAsync` | `POST /internal/integration/storage/upload` → `Vitals.ObjectStorage` | `ObjectStorage:UseStub: true` |
| Pharmacy / Lab | `IntegrationDispatchService` | Принимает заказы, без внешних API | — |
| Emergency dispatch | `IntegrationDispatchService`, `EmergencyRequiredConsumerHostedService` | Consumer `emergency_required` + HTTP stub | `Kafka:Enabled: false` |
| Lab orders (Kafka) | `LabOrderRequiredConsumerHostedService` | Consumer `lab.order_required` | `Kafka:Enabled: false` |

Порт по умолчанию: **5270**. Health: `GET /internal/integration/health`.

---

## PaymentService

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| Payment gateway | `IntegrationPaymentGatewayClient` | HTTP → Integration `payments/process` | `IntegrationService:BaseUrl` |
| Kafka events | `KafkaPaymentEventPublisher` | `payment.completed` / `payment.failed` | `Kafka:Enabled: false` |

Порт: **5280**. Gateway: `POST /api/v1/payments`.

---

## AnalyticsService / QualityService

| Заглушка / упрощение | Описание | Конфиг |
| --- | --- | --- |
| Kafka consumers | Метрики из `consultation.completed`, `prescription.issued`, `triage.completed`, `payment.completed` | `Kafka:Enabled: false` |
| Dashboard / metrics API | `GET /api/analytics/dashboard`, `GET /api/quality/metrics` | — |

Порты: **5290** (Analytics), **5295** (Quality).

---

## Object Storage (`Vitals.ObjectStorage`)

| Заглушка / упрощение | Класс / файл | Описание | Конфиг |
| --- | --- | --- | --- |
| In-memory stub | `StubObjectStorageProvider` | Fake URLs для dev | `ObjectStorage:UseStub: true` |
| S3-compatible | `S3ObjectStorageProvider` | AWSSDK.S3 (MinIO/Yandex S3) | `ObjectStorage:ServiceUrl`, `BucketName`, keys |
| MR attachments | `PatientAttachmentService` | `POST/GET .../attachments` | MedicalRecord `ObjectStorage` section |

---

## Межсервисные разрывы (интеграции-«заглушки»)

Связи, которые в архитектуре описаны через Kafka/HTTP. **Kafka-цепочка реализована** (Confluent), но по умолчанию `Kafka:Enabled: false` — нужен `docker-compose.infra.yml` и включение в конфиге.

| Поток | Ожидание | Сейчас |
| --- | --- | --- |
| AI Triage → Routing | `triage.completed` (Kafka) | **Confluent producer/consumer**; fallback — POST `/internal/routing/triage-completed` |
| Routing → Consultation | `routing.decision` (Kafka) | **Confluent producer/consumer**; fallback — POST `/internal/consultations/routing-decision` |
| Consultation → Medical Record | HTTP append events | **Работает**; при ошибке MR — warning в лог |
| Consultation → Notification | `consultation.created` | **Confluent producer/consumer**; fallback — POST `/internal/notifications/events` |
| Prescription → Notification | `prescription.issued`, `prescription.expiring_soon` | **Confluent producer**; Notification consumer или POST |
| Prescription → Integration | отправка в аптеку / ЕГИСЗ | Pharmacy **stub**; ЕГИСЗ через Integration (stub) |
| Payment → Integration | `payments/process` | **Stub** auto-complete |
| Payment / Consultation → Analytics | Kafka topics | **Consumer** при `Kafka:Enabled: true` |
| Consultation → Quality | `consultation.completed` | **Consumer** при `Kafka:Enabled: true` |
| Prescription → Medical Record | HTTP append | **Работает** при подписании/выдаче |
| Routing → Integration | `lab.order_required`, `emergency_required` | **Confluent consumer** в IntegrationService; dispatch — stub |
| Routing → Notification | `auto_response_required` | **Confluent producer/consumer** |
| Routing / Triage → Medical Record | HTTP контекст пациента | **Работает** (`GET internal/medical-records/patients/{id}/state`); при недоступности MR — routing/triage продолжают с `null` контекстом |

---

## Dev-only поведение (не для production)

| Где | Что происходит |
| --- | --- |
| AITriageService, RoutingService, MedicalRecordService, ConsultationService, PrescriptionService, NotificationService, IntegrationService, UserService | JWT bypass **только** при `AllowInsecureDevelopmentBypass: true` в `appsettings.Development.json` |
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
- [ConsultationService](ConsultationService.md) — WebRTC P2P / SFU, SignalR, Kafka
- [Frontend-TZ-Video.md](Frontend-TZ-Video.md) — ТЗ фронта видео-консультаций
- [PrescriptionService](PrescriptionService.md) — validation, e-sign, pharmacy stub
- [NotificationService](NotificationService.md) — channels, templates, Kafka
