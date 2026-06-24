# Ревью соответствия VitalsBackend техническому заданию

**Дата:** 2026-06-01 (обновлено: 2026-06-01, Фаза 1 + Фаза 2 + Фаза 3)  
**Источник ТЗ:** `d:\programming\Vitals\Product\ТЗ для Backend.docx`  
**Репозиторий:** `VitalsBackend` (ветка текущей разработки)  
**Связанные документы:** [Stubs.md](./Stubs.md), [README.md](../README.md)

---

## Краткое резюме

| Область | Оценка | Комментарий |
|---------|--------|-------------|
| Каркас микросервисов (12 из 12+) | **В основном** | Gateway, Auth, User, MR, Triage, Routing, Consultation, Prescription, Notification, Integration, **Payment, Analytics, Quality** |
| API Gateway как единая точка входа | **Частично** | Контроллеры и валидация есть; ответы — прозрачный прокси без Gateway DTO на выходе; YARP для ESIA/SignalR |
| Event-driven (Kafka) | **Частично** | Confluent producer/consumer; `Kafka:Enabled: false` по умолчанию |
| Integration Service | **Частично** | :5270; payment/voice/EGISZ/storage endpoints; провайдеры — stub |
| Emergency handler | **Частично** | Consumer в IntegrationService; dispatch — stub |
| Payment / Analytics / Quality | **Частично** | Каркас Фазы 3 (:5280, :5290, :5295); Kafka off by default |
| ML / SFU / КЭП / расписание | **Частично** | HTTP-адаптеры (Фаза 2); по умолчанию stub/fallback |
| Notification DLQ + voice | **Частично** | DLQ таблица + escalation через Integration voice stub |
| Object storage / CDN | **Частично** | `Vitals.ObjectStorage`; MR attachments + Integration upload |
| Medical Record как SoT | **В основном соответствует** | Event store, проекции, HTTP-интеграции работают |
| Production-безопасность | **Частично** | `AddVitalsAuthentication`; dev bypass только в Development config |

**Общий вывод:** MVP с **event-driven каркасом** и **клиническими HTTP-адаптерами** (ML, SFU, КЭП, расписание). E2E автоматизируется при включении Kafka и подключении внешних провайдеров. Production требует credentials, нагрузочных тестов и Gateway response DTO.

---

## 1. Покрытие сервисов из ТЗ

| Сервис по ТЗ | В репозитории | Порт (dev) | Статус |
|--------------|---------------|------------|--------|
| API Gateway | `src/Services/ApiGateway` | 5080 | Реализован (BFF + частичный YARP) |
| User Service | `src/Services/UserService` | 5195 | Реализован |
| Auth Service | `src/Services/AuthService` | 5200 | Реализован |
| AI Triage Service | `src/Services/AITriageService` | 5220 | Реализован (ML — stub) |
| Medical Record Service | `src/Services/MedicalRecordService` | 5210 | Реализован |
| Routing Service | `src/Services/RoutingService` | 5230 | Реализован (внутренний, без Gateway) |
| Consultation Service | `src/Services/ConsultationService` | 5240 | Реализован |
| Prescription Service | `src/Services/PrescriptionService` | 5250 | Реализован |
| Notification Service | `src/Services/NotificationService` | 5260 | Реализован (каналы — stub) |
| **Integration Service** | `src/Services/IntegrationService` | 5270 | **Scaffold** (payment/voice/EGISZ/storage + SMS/email/push stub) |
| **Emergency handler** | `IntegrationService` (consumer) | — | **Scaffold** (`emergency_required` → dispatch stub) |
| **Payment Service** | `src/Services/PaymentService` | 5280 | **Каркас** (Integration gateway + Kafka events) |
| **Analytics Service** | `src/Services/AnalyticsService` | 5290 | **Каркас** (Kafka consumers + dashboard API) |
| **Quality Service** | `src/Services/QualityService` | 5295 | **Каркас** (consultation.completed → quality scores) |

---

## 2. API Gateway (.NET)

### Соответствует ТЗ

- Единая точка входа `/api/v1/*` для клиентов ([README.md](../README.md)).
- Контроллеры по доменам: Auth, Users, Admin/Roles, Doctors, Medical Records, Triage, Consultations, Prescriptions, Notifications (`ApiGateway.API/Controllers/V1/`).
- Входные DTO и FluentValidation на Gateway (`ApiGateway.Application/DTOs/`, `ApiGateway.API/Validators/GatewayValidators.cs`).
- Rate limiting: sliding window, политики 100/1000/10 login/5 register (`appsettings.json`), 429 + `Retry-After`.
- Middleware: логирование, JWT, rate limit, сжатие, X-Request-ID, global exception handler (`Program.cs`).
- YARP для ESIA OAuth и SignalR consultation hub.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| G-1 | **Критично** | Ни один внутренний сервис не доступен из внешней сети напрямую | Все сервисы слушают порты локально; **нет сетевой изоляции** (Docker/K8s network policies не описаны). UserService (5195) доступен без JWT. |
| G-2 | **Критично** | Gateway не передаёт внутренние сущности; отдельные DTO на **запрос и ответ**, агрегация из нескольких сервисов | `IBackendForwarder` **прозрачно проксирует** JSON backend → client. Gateway DTO только на входе. Агрегация — только в `DoctorsController` (поиск). |
| G-3 | **Существенно** | Rate limit counters в **Redis** для нескольких экземпляров Gateway | По умолчанию `UseInMemoryFallback: true` — in-memory store (`InMemorySlidingWindowRateLimitStore`). Redis опционален. |
| G-4 | **Существенно** | Middleware логирования **исключает** пароли и токены | `RequestLoggingMiddleware` логирует path/method/status/IP, но **не маскирует** Authorization и body. |
| G-5 | **Существенно** | Бизнес-правила валидации (нельзя записаться на прошедшую дату и т.д.) | Валидаторы покрывают формат полей; **нет** правил «дата в будущем» для расписания/консультаций. |
| G-6 | **Существенно** | Контроллер врачей: поиск, карточки, **реальное расписание** | `DoctorsController.GetSchedule` **проксирует** UserService `api/doctors/{id}/schedule` (таблица `DoctorScheduleSlots`). Fallback stub в Routing при `UseStubSchedule: true`. |
| G-7 | **Средне** | Все публичные эндпоинты через Gateway | Routing Service **не** зарегистрирован в Gateway (по задумке internal-only, но публичные `/api/routing/*` доступны напрямую на :5230). |
| G-8 | **Средне** | ESIA через Gateway с полным BFF-слоем | ESIA идёт **напрямую через YARP** без Gateway DTO/валидации. |
| G-9 | **Низко** | Dev ephemeral JWT key | `JwtSigningKeyProvider` генерирует временный ключ, если JWKS недоступен — **не для production**. |

---

## 3. User Service

### Соответствует ТЗ

- Отдельная PostgreSQL, профили Patient/Doctor/Organization.
- CRUD профилей, admin search/block, roles/permissions.
- AES-шифрование чувствительных полей (полис, СНИЛС).
- Internal API `/internal/users/*`, `/internal/doctors/find-available` для Routing.
- **Расписание врачей:** `DoctorScheduleSlots`, `GET api/doctors/{id}/schedule`, seeder на старте.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| U-1 | **Существенно** | REST API для **внутреннего** использования через Gateway | `AddVitalsAuthentication` + `UseVitalsInternalServiceAuth` на `/internal/*`. Публичные `/api/users/*` — JWT. |
| U-2 | **Существенно** | Роли: пациент, врач, админ клиники, лаборатория, аптека, админ платформы, **суперадмин** | Роли и permissions реализованы, но покрытие всех TZ-scopes (`finance.reports.read`, `lab.results.upload` и др.) **требует проверки seed/БД** — не все могут быть заведены. |
| U-3 | **Существенно** | Шифрование через KMS/HSM | `EncryptionService` — статический ключ из config с padding hack, не KMS. |
| U-4 | **Существенно** | Настройки уведомлений в User Service, кэш в Notification | Notification хранит prefs **локально**, не синхронизирует с User Service live. |
| U-5 | **Средне** | Каскадное/юридическое удаление с подтверждением | Soft-delete/block есть; полный GDPR erase flow **не документирован**. |
| U-6 | **Средне** | Рейтинг врача, список пациентов на обслуживании | Поля профиля врача частично есть; **рейтинг и patient list** могут быть не заполнены/не обновляются из Consultation. |
| U-7 | **Низко** | `AssignedBy` при назначении роли | В `RolesController` — `AssignedBy = null`. |

---

## 4. Auth Service

### Соответствует ТЗ

- Register/login/refresh/logout, change-password, forgot/reset password.
- Argon2id, RS256 JWT, refresh tokens в БД с fingerprint/IP.
- Access token 15 мин, refresh 30 дней (`AccessTokenMinutes: 15`).
- JWKS `/internal/jwks`, validate `/internal/token/validate`.
- ESIA OAuth код (`EsiaController`, `EsiaOAuthService`), таблица `esia_links`.
- Разделение БД Auth vs User.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| A-1 | **Критично** | Восстановление пароля через **SMS/email** | Код сброса **логируется**, не отправляется (`AuthenticationService`). |
| A-2 | **Существенно** | ESIA OAuth production-ready | `Esia:Enabled: false`, пустые credentials → 503. |
| A-3 | **Существенно** | Стабильный RSA ключ (не ephemeral) | `RsaKeyProvider` генерирует ephemeral ключ без `Jwt:RsaPrivateKeyPem`. |
| A-4 | **Средне** | Связывание аккаунта ESIA + телефон/пароль | Код linking есть; **не протестирован** end-to-end без реального ESIA. |
| A-5 | **Низко** | Scopes в JWT из User Service | Роли кэшируются при login; **обновление прав без re-login** не описано. |

---

## 5. AI Triage Service

### Соответствует ТЗ

- Сессии triage, сообщения, pipeline обработки.
- Чтение контекста из Medical Record (HTTP).
- Публикация `triage.completed` (stub).
- Gateway `/api/v1/triage/sessions/*`.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| T-1 | **Существенно** | NER + LLM (внешние ML или встроенные модели) | `HttpNerService` / `HttpLlmTriageService` при `UseStubModels: false`; fallback на stub. **Нужен** развёрнутый ML endpoint. |
| T-2 | **Существенно** | Публикация `triage.completed` в **Kafka** для Routing | `KafkaTriageEventPublisher` + consumer в Routing; **включить** `Kafka:Enabled: true`. |
| T-3 | **Существенно** | Учёт полной истории диалога + MR | Реализовано частично; stub LLM не даёт клинической точности. |
| T-4 | **Существенно** | EmergencyWarning → цепочка до скорой | Флаг в LLM; `EmergencyRequiredConsumerHostedService` в IntegrationService (dispatch stub). |
| T-5 | **Низко** | Dev JWT bypass | Только через `appsettings.Development.json` (`AllowInsecureDevelopmentBypass`). |

---

## 6. Medical Record Service

### Соответствует ТЗ

- Event Store, snapshots, projections, access grants, audit.
- Source of Truth для медицинских данных.
- Public JWT API + internal service API.
- HTTP append из Consultation/Prescription.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| MR-1 | **Критично** | Запись через **Kafka** (Consultation → Kafka → MR) | Consultation пишет в MR **по HTTP**; Kafka publisher — log stub. |
| MR-2 | **Существенно** | Integration Service → `lab.result_ready` → MR | Integration Service **отсутствует**; topic `integration.lab-results` не используется. |
| MR-3 | **Существенно** | Шардирование, партиционирование Kafka по patientId | Не реализовано (Kafka off). |
| MR-4 | **Средне** | Object storage для файлов/снимков | Ссылки в событиях; **S3/CDN интеграция** не видна в коде. |
| MR-5 | **Средне** | Dev JWT bypass + dev headers на internal API | `X-Service-Name`, `X-User-Id` без mTLS. |

---

## 7. Routing Service

### Соответствует ТЗ

- Rule-based engine, clinic rules seed, audit log.
- Internal `POST /internal/routing/triage-completed`.
- Публикация `routing.decision`, `lab.order_required`, `emergency_required`, `auto_response_required` (stub).
- Public JWT `/api/routing/*`.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| R-1 | **Критично** | Consumer `triage.completed` из Kafka | `TriageCompletedConsumerHostedService` — warning «Confluent not implemented». |
| R-2 | **Критично** | Emergency handler + Integration → скорая | `emergency_required` → **log only**; handler **отсутствует**. |
| R-3 | **Критично** | Latency ≤500ms p95, 100 events/s | Не измерено; без Kafka end-to-end **не проверяемо**. |
| R-4 | **Существенно** | Redis для stateful маршрутов + replay из Kafka | Redis disabled; state в PostgreSQL only. |
| R-5 | **Существенно** | Реальное расписание врачей (1000 врачей, ≤100ms search) | `UserServiceDoctorScheduler` → UserService `DoctorScheduleSlots`; fallback `StubDoctorScheduler`. |
| R-6 | **Существенно** | Сложные multi-step маршруты | Базовый rule engine; **нет** state machine для цепочек шагов из ТЗ. |
| R-7 | **Существенно** | Hot-reload правил без перезапуска | Правила в PostgreSQL; **dynamic reload** не подтверждён. |
| R-8 | **Средне** | Не доступен клиенту напрямую | Public API на :5230 **в обход Gateway**. |
| R-9 | **Средне** | Fallback сценарии (нет врачей → терапевт) | Частично в rules; не все TZ-fallback покрыты. |

---

## 8. Consultation Service

### Соответствует ТЗ

- Жизненный цикл сессии (CREATED → … → COMPLETED).
- REST chat + SignalR hub (`/hubs/consultation`).
- HTTP append в Medical Record.
- Internal routing-decision endpoint, emergency trigger.
- Timeout hosted service (базовый).

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| C-1 | **Критично** | Автосоздание сессии из Kafka `routing.decision` | Consumer — stub; нужен manual `POST /internal/consultations/routing-decision`. |
| C-2 | **Существенно** | SFU/WebRTC видео (Selective Forwarding Unit) | `LiveKitSfuSignalingService` / `HttpSfuSignalingService` при `Sfu:UseStub: false`; stub по умолчанию. |
| C-3 | **Критично** | Kafka: `consultation.created`, MR через Kafka | Publisher — log stub; MR через **HTTP**, не Kafka. |
| C-4 | **Существенно** | Redis для активных сессий и offline messages | `InMemorySessionStateStore`, Redis off. |
| C-5 | **Существенно** | Напоминания 60/15/5 мин, push/SMS/email | **Нет** `consultation.reminder`; только expiry scan. |
| C-6 | **Существенно** | Статусы PAUSED, DOCTOR_LEFT, консilium | Не все TZ-статусы и сценарии реализованы. |
| C-7 | **Существенно** | Запись видео, screen share, virtual background | **Не реализовано** (stub SFU). |
| C-8 | **Существенно** | Оценка качества → Analytics Service | Analytics Service **отсутствует**. |
| C-9 | **Существенно** | Юридический эпикриз + УКЭП | Протокол подписывается через `Vitals.ESignature` при `CompleteAsync`; полный эпикриз/УКЭП провайдер — stub по умолчанию. |
| C-10 | **Существенно** | Performance: 10k WebSocket, p95 chat ≤100ms | **Не верифицировано** нагрузочным тестом. |
| C-11 | **Средне** | Dev JWT bypass | Как в других сервисах. |

---

## 9. Prescription Service

### Соответствует ТЗ

- Draft → sign → pharmacy flow, rule-based validation.
- QR (QRCoder), patient instructions.
- HTTP read/append Medical Record.
- Expiry hosted service, Gateway routes.

### Несоответствия и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| P-1 | **Существенно** | Электронная подпись рецепта (КЭП/УКЭП) | `PrescriptionESignatureAdapter` → `Vitals.ESignature.HttpESignatureProvider`; stub по умолчанию. |
| P-2 | **Критично** | Отправка в аптеку через Integration Service | `StubPharmacyIntegrationClient`. |
| P-3 | **Существенно** | Проверка льгот через ЕГИСЗ | Флаг в БД only; **нет** Integration. |
| P-4 | **Существенно** | Kafka events: `prescription.created`, `expiring_soon` | Log stub only. |
| P-5 | **Существенно** | Проверки: FDA pregnancy category, duplicate meds, MR cache in Redis | Rule engine базовый; **Redis cache MR** не подтверждён. |
| P-6 | **Существенно** | Повторные/пролонгированные рецепты | **Не реализовано** или частично. |
| P-7 | **Существенно** | Analytics отчёты для врачей/клиник | **Отсутствует**. |
| P-8 | **Средне** | `StubUserPermissionClient` | Права врача не из live User Service. |

---

## 10. Notification Service

### Соответствует ТЗ

- Template engine (~25 ru templates), dedup, retries.
- Push tokens, preferences, history API.
- Internal event ingestion `POST /internal/notifications/events`.
- Gateway `/api/v1/notifications/*`.

### Несоходства и недочёты

| # | Серьёзность | Требование ТЗ | Факт в коде |
|---|-------------|---------------|-------------|
| N-1 | **Критично** | Kafka consumer для всех TZ-topics | `KafkaNotificationConsumerHostedService` — stub; events через manual HTTP. |
| N-2 | **Критично** | APNS, FCM, SMS, email, voice через Integration | `ChannelDispatchers` — **log only**. |
| N-3 | **Существенно** | Redis cache templates + user prefs from User Service | Redis disabled; prefs local table. |
| N-4 | **Существенно** | Dead letter queue после 5 попыток | Retry hosted есть; **DLQ таблица/очередь** не найдена. |
| N-5 | **Существенно** | События: `payment.failed`, `system.maintenance`, `lab.result_critical` | Templates/events **не все** из TZ перечня. |
| N-6 | **Существенно** | Performance: 1000 events/s, ≤100ms latency | **Не верифицировано**. |
| N-7 | **Средне** | Email digest, 5-year legal retention | **Не реализовано**. |
| N-8 | **Средне** | Dev JWT bypass | Как в других сервисах. |

---

## 11. Integration Service (отсутствует)

ТЗ описывает полноценный сервис-адаптер с:

- Лаборатории, аптеки, МИС, платежи, SMS/email/push, гос. системы (ЕСИА, ЕГИСЗ, Честный ЗНАК, ОМС).
- Outbox pattern, retry/backoff, DLQ, mapping tables, webhook ingress.
- 500 concurrent outbound requests, healthcheck per integration.

**Факт:** проекта `IntegrationService` **нет**. Есть только:

- `StubPharmacyIntegrationClient` в PrescriptionService.
- `IntegrationService:BaseUrl` + stub dispatchers в NotificationService.
- URL `http://localhost:5270` в config без реализации.

**Влияние:** блокирует pharmacy, lab orders, SMS/email/voice, payments, ESIA через единый слой, emergency API.

---

## 12. Emergency handler (отсутствует)

ТЗ (Routing §Функция 1, Consultation §Функция 10):

> «Специальный обработчик (отдельный микросервис для экстренных случаев) вызывает скорую помощь через API»

**Факт:**

- Routing публикует `emergency_required` в log.
- Consultation имеет `POST .../emergency` → log + MR event.
- **Нет** consumer, **нет** вызова внешнего API скорой/112.

---

## 13. Сквозная архитектура (Kafka и интеграции)

### Ожидаемый поток по ТЗ

```
Patient → Gateway → Triage → [Kafka: triage.completed] → Routing
  → [Kafka: routing.decision] → Consultation → [Kafka: consultation.created] → Notification
  → [HTTP/Kafka] → Medical Record
Routing → [Kafka: lab.order_required] → Integration → Lab
Prescription → [Kafka: prescription.issued] → Integration → Pharmacy → Notification
```

### Фактический поток

```
Patient → Gateway → Triage → (log only)
Manual POST /internal/routing/triage-completed → Routing → (log only)
Manual POST /internal/consultations/routing-decision → Consultation
Consultation → HTTP → Medical Record ✓
Manual POST /internal/notifications/events → Notification → (log channels)
Prescription → HTTP → Medical Record ✓
```

| Разрыв | Критичность |
|--------|-------------|
| Kafka disabled everywhere | **Критично** |
| No Confluent.Kafka implementation | **Критично** |
| Integration Service missing | **Критично** |
| Emergency handler missing | **Критично** |
| Payment/Analytics/Quality services missing | **Существенно** |
| Root `docker-compose.yml` — full stack (13 сервисов + infra) | **Решено** |

---

## 14. Безопасность и compliance

| # | Требование | Статус |
|---|------------|--------|
| S-1 | JWT RS256, 15 min access | Да — AuthService |
| S-2 | Production JWT validation без bypass | Нет — dev bypass в 6+ сервисах + Gateway ephemeral key |
| S-3 | UserService за network boundary / auth | Нет — open API |
| S-4 | Internal APIs с service tokens/mTLS | Нет — trust-by-network |
| S-5 | Secrets в Vault, не в appsettings | Нет — keys in config |
| S-6 | Password reset не в логах | Нет — logged |
| S-7 | TLS everywhere, DTLS WebRTC | Частично — TLS assumed; WebRTC stub |
| S-8 | GDPR: export notification history | Частично — history API есть; полный GDPR flow — нет |
| S-9 | 152-ФЗ: шифрование ПДн | Частично — AES in UserService, не KMS |

---

## 15. Инфраструктура и эксплуатация

| Аспект | ТЗ | Код |
|--------|-----|-----|
| PostgreSQL per service | Да | Да — EF Core + Migrate on startup |
| Redis (Gateway rate limit, Routing state, Consultation sessions, Notification cache) | Да | Частично — optional/disabled, in-memory fallbacks |
| Kafka cluster | Да | Нет — disabled by default (в Docker compose включён) |
| Observability (metrics, tracing beyond X-Request-ID) | Подразумевается | Частично — logging only, no Prometheus/OpenTelemetry in review scope |
| CI/CD, K8s manifests | Не детализировано в ТЗ | Частично — `docker-compose.yml` в корне (full stack) |
| Legacy `ApiGateway/Solution1/` | — | Legacy — мусорный scaffold, не используется |

---

## 16. Сводная матрица критичности

| Критичность | Кол-во | Примеры |
|-------------|--------|---------|
| **Критично** | ~15 | Gateway pass-through, Payment/Analytics missing, ML/SFU/КЭП без реальных провайдеров, pharmacy stub |
| **Существенно** | ~30 | Kafka off by default, Redis fallbacks, consultation reminders, ESIA off, MR Kafka path |
| **Средне** | ~20 | Dev bypass, partial consultation statuses |
| **Низко / tech debt** | ~10 | AssignedBy null, docs drift (Stubs.md ServiceInfoControllers), legacy Solution1 |

---

## 17. Рекомендуемый порядок устранения

### Фаза 1 — Блокеры production

1. Включить **Confluent.Kafka** producer/consumer во всех сервисах; связать triage → routing → consultation → notification.
2. Реализовать **Integration Service** (минимум: SMS, email, push, pharmacy, lab).
3. Реализовать **Emergency handler** (consumer `emergency_required`).
4. Убрать **dev JWT bypass**; добавить auth на UserService и internal APIs (service JWT/mTLS).
5. Gateway: **response DTO mapping** или явная документация контракта; Redis обязателен для rate limit в prod.

### Фаза 2 — Клинический контур (каркас)

6. **ML adapters:** `HttpNerService`, `HttpLlmTriageService` (`MlServices:UseStubModels: false` + endpoints).
7. **SFU/WebRTC:** `LiveKitSfuSignalingService`, `HttpSfuSignalingService` (`Sfu:UseStub: false`).
8. **Doctor scheduling:** `DoctorScheduleSlots` в UserService; Routing → `UserServiceDoctorScheduler`; Gateway проксирует расписание.
9. **E-signature:** shared `Vitals.ESignature` для рецептов и протоколов консультаций.

**Остаётся для production Фазы 2:** развернуть ML/SFU/КЭП провайдеры, `UseStubSchedule: false`, нагрузочные тесты SLA.

### Фаза 3 — Полнота ТЗ (каркас)

10. **Payment Service** (:5280) + Integration `payments/process` adapter.
11. **Analytics / Quality services** (:5290 / :5295) + Kafka consumers + Gateway proxy.
12. **Notification:** полный набор events (`consultation.completed`, `prescription.expired`, `emergency.required`, `payment.completed`), **DLQ** (`dead_letter_notifications`), **voice escalation** через Integration.
13. **ЕГИСЗ** (`egisz/preferential/check` + Prescription validation), **object storage/CDN** (`Vitals.ObjectStorage`, MR attachments, Integration upload).
14. **Нагрузочные тесты SLA** — `tests/load/k6/` (Routing 500ms, Consultation chat 100ms, Notification 1000/s).

**Остаётся для production Фазы 3:** реальные payment/EGISZ/S3/CDN провайдеры, `Kafka:Enabled: true`, прогон k6 на staging, Gateway response DTO.

---

## 18. Положительные стороны реализации

Несмотря на разрывы с production-ТЗ, кодовая база имеет сильный фундамент:

- Чистая **Clean Architecture** (API / Application / Domain / Infrastructure) во всех сервисах.
- **Medical Record** как event-sourced SoT — архитектурно верное ядро.
- **ApiGateway BFF** с typed controllers, validators, forwarder — правильное направление (нужно довести response DTO).
- Подробная **документация** (`docs/*.md`, `Stubs.md`) с честной фиксацией заглушек.
- Auth: Argon2id + RS256 + refresh rotation — соответствует best practices.
- Consultation SignalR + MR HTTP integration — рабочий вертикальный срез для demo.

---

## Приложение A. Ссылки на ключевые файлы

| Компонент | Путь |
|-----------|------|
| Gateway controllers | `src/Services/ApiGateway/ApiGateway.API/Controllers/V1/` |
| Gateway forwarder | `src/Services/ApiGateway/ApiGateway.Infrastructure/Clients/BackendForwarder.cs` |
| Stubs registry | `docs/Stubs.md` |
| Kafka logging publisher (пример) | `src/Services/MedicalRecordService/.../LoggingMedicalRecordEventPublisher.cs` |
| Kafka consumer stub (пример) | `src/Services/RoutingService/.../TriageCompletedConsumerHostedService.cs` |
| Doctor schedule stub | `src/Services/ApiGateway/.../DoctorsController.cs` → UserService |
| ML HTTP adapters | `src/Services/AITriageService/.../HttpNerService.cs`, `HttpLlmTriageService.cs` |
| SFU adapters | `src/Services/ConsultationService/.../LiveKitSfuSignalingService.cs` |
| E-signature shared lib | `src/Shared/Vitals.ESignature/` |
| Doctor schedule (UserService) | `src/Services/UserService/.../DoctorScheduleService.cs` |
| Payment Service | `src/Services/PaymentService/` |
| Analytics / Quality | `src/Services/AnalyticsService/`, `src/Services/QualityService/` |
| Object storage shared lib | `src/Shared/Vitals.ObjectStorage/` |
| k6 SLA load tests | `tests/load/k6/` |
| Notification channel stubs | `src/Services/NotificationService/.../ChannelDispatchers.cs` |
| Prescription client stubs | `src/Services/PrescriptionService/.../PrescriptionClients.cs` |

---

*Документ сгенерирован автоматически на основе сравнения ТЗ (`ТЗ для Backend.docx`) с состоянием репозитория на дату ревью. Для актуализации после доработок обновите разделы 2–13 и матрицу §16.*
