# Routing Service

Диспетчер экосистемы Vitals: принимает результат AI Triage (`triage.completed`), принимает решение о маршруте пациента и публикует события для Consultation, Integration и Notification сервисов. **Не взаимодействует с пациентом напрямую.**

## Компоненты (текущая версия)

| Компонент | Реализация |
| --- | --- |
| Движок маршрутизации | Rule-based (`RuleBasedRoutingEngine`, версия `routing-rules-v1`) |
| Расписание врачей | **Заглушка** (`StubDoctorScheduler`, 10 врачей) |
| Medical Record | HTTP → `internal/medical-records/patients/{id}/state` |
| Kafka consumer | Confluent при `Kafka:Enabled`; иначе AITriage бьёт в `POST /internal/routing/triage-completed` |
| Kafka publisher | При `Enabled: false` — HTTP в Consultation + Prescription (lab-orders) |
| Состояние маршрутов | PostgreSQL (`patient_routes`) |
| Правила клиники | PostgreSQL + seed (`clinic_routing_rules`) |
| Аудит решений | PostgreSQL (`routing_audit_entries`, append-only) |

## Исходы маршрутизации

| Outcome | Событие Kafka |
| --- | --- |
| `AutoResponse` | `auto_response_required` |
| `Consultation` | `routing.decision` |
| `LabsBeforeConsultation` | `lab.order_required` + `routing.decision` |
| `HomeVisit` | `routing.decision` |
| `Emergency` | `emergency_required` |

## API

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/internal/routing/triage-completed` | Обработать событие triage (dev / без Kafka) |
| GET | `/internal/routing/decisions/{id}` | Решение по ID (internal) |
| GET | `/api/routing/decisions/{id}` | Решение по ID (JWT), включая `recommendedLabs` |
| GET | `/api/routing/patients/{id}/active-route` | Активный маршрут + `currentDecisionId` (JWT) |
| POST | `/internal/routing/patients/{id}/labs` | Анализы из протокола консультации → шаг + merge labs в decision |

`active-route` создаётся для любого outcome (не только LabsBeforeConsultation). Если route
ещё нет, но есть decision — ответ собирается (heal) из последнего decision.

## Пример (ОРВИ, urgency 3)

```json
POST /internal/routing/triage-completed
{
  "sessionId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "patientId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "urgencyLevel": 3,
  "emergencyWarning": false,
  "patientMessageSummary": "болит горло, температура 38.2, кашель, третий день",
  "patientAgeYears": 34,
  "hypotheses": [{ "condition": "ОРВИ", "probability": 0.9 }]
}
```

Ответ: `Consultation`, specialist `therapist`, format `SyncChat`, врач с минимальной загрузкой, событие `routing.decision`.

## Fallback

- Нет данных триажа → терапевт
- Нет свободного узкого специалиста → терапевт
- Некорректная urgency без emergency → cap до 3
- EmergencyWarning или urgency 5 → `emergency_required`

## Запуск

```bash
cd src/Services/RoutingService
docker compose up -d postgres-routing
dotnet run --project RoutingService.API
```

Swagger: [http://localhost:5230/swagger](http://localhost:5230/swagger)  
PostgreSQL: порт **5436**

## Дальше

- Confluent consumer для `triage.completed`
- Confluent producer для исходящих топиков
- Redis-кэш активных маршрутов
- Плагины правил и admin UI для клиник
- (сделано) HTTP fallback triage→route→consultation без Kafka
