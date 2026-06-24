# Consultation Service

Коммуникационное ядро Vitals: сессии консультаций, чат, видео-сигналинг, жизненный цикл, протоколы и интеграция с Medical Record.

## Компоненты (текущая версия)

| Компонент | Реализация |
| --- | --- |
| Создание сессии | Из `routing.decision` (internal HTTP) или вручную |
| Жизненный цикл | `CREATED` → `PATIENT_JOINED` → `DOCTOR_JOINED` → `ACTIVE` → … → `COMPLETED` |
| Чат | REST + **SignalR** (`/hubs/consultation`) |
| Видео | **Заглушка SFU** (`StubSfuSignalingService`) — только сигналинг |
| Medical Record | HTTP → `internal/medical-records/patients/{id}/events` |
| Kafka consumer | **Заглушка** (`routing.decision`) |
| Kafka publisher | **Заглушка** (`consultation.created`, `consultation.completed`, …) |
| Redis | **Заглушка** (`InMemorySessionStateStore`) |
| Таймауты | `SessionTimeoutHostedService` (scan каждую минуту) |

## Статусы

`Created`, `PatientJoined`, `DoctorJoined`, `Active`, `Paused`, `DoctorLeft`, `Completed`, `Cancelled`, `Expired`

## API (JWT)

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/api/consultations` | Создать сессию вручную |
| GET | `/api/consultations/{id}` | Сессия |
| POST | `/api/consultations/{id}/join` | Подключиться (`role`: Patient/Doctor) |
| POST | `/api/consultations/{id}/consent` | Согласие на обработку данных |
| GET/POST | `/api/consultations/{id}/messages` | История / отправка |
| POST | `/api/consultations/{id}/pause`, `/resume` | Пауза |
| POST | `/api/consultations/{id}/doctor-leave` | Врач завершил |
| POST | `/api/consultations/{id}/complete` | Протокол консультации |
| POST | `/api/consultations/{id}/confirm` | Подтверждение пациента |
| POST | `/api/consultations/{id}/cancel` | Отмена |
| POST | `/api/consultations/{id}/video/start` | Комната SFU (stub) |
| POST | `/api/consultations/{id}/emergency` | Экстренный протокол |
| POST | `/api/consultations/{id}/ratings` | Оценка после завершения |
| POST | `/api/consultations/{id}/invite-doctor` | Консилиум |

Internal (без JWT):

| Метод | Путь |
| --- | --- |
| POST | `/internal/consultations/routing-decision` |

## SignalR

- Hub: `/hubs/consultation`
- Через Gateway: `/api/v1/consultations/hub?access_token=...`
- Методы: `JoinSession(sessionId)`, `LeaveSession(sessionId)`
- События: `messageReceived`, `statusChanged`

## Поток из Routing

```
POST /internal/consultations/routing-decision
{
  "patientId": "...",
  "sessionId": "...",
  "doctorId": "...",
  "doctorName": "Иванова И.И.",
  "consultationFormat": "SyncChat",
  "effectiveUrgencyLevel": 3,
  "outcomeType": "Consultation"
}
```

→ `consultation.created` (лог) + событие `ConsultationStarted` в Medical Record.

## Запуск

```bash
cd src/Services/ConsultationService
docker compose up -d postgres-consultation
dotnet run --project ConsultationService.API
```

Swagger: [http://localhost:5240/swagger](http://localhost:5240/swagger)  
PostgreSQL: порт **5437**  
Gateway: `/api/v1/consultations/*`

## Дальше

- Confluent consumer для `routing.decision`
- Реальный SFU / WebRTC
- Redis для hot state и unread counters
- Outbox для Medical Record при недоступности
- Push/SMS напоминания (Notification Service)
