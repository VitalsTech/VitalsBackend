# Medical Record Service

Единственный **Source of Truth** для медицинских данных Vitals. Другие сервисы не хранят медицинскую информацию в своих БД (допустимы только кэши/проекции).

## Архитектура данных

| Слой | Таблицы | Назначение |
|------|---------|------------|
| Event Store | `medical_events` | Append-only поток событий пациента (immutable) |
| Snapshots | `patient_snapshots` | Сериализованное состояние каждые N событий |
| Projections | `projection_*` | Быстрое чтение текущего состояния |
| Access | `access_grants` | Согласия и доступ врачей/клиник |
| Audit | `audit_logs` | Неизменяемый журнал чтений и записей |

## Типы событий

`PatientRequestCreated`, `AiTriageUrgencyDetermined`, `DiagnosisConfirmed`, `DiagnosisRevised`, `PrescriptionIssued`, `PrescriptionRevoked`, `LabResultReceived`, `TreatmentStarted`, `TreatmentCompleted`, `AllergyRecorded`, `VitalSignRecorded`, `ImmunizationRecorded`

## API

Базовый префикс: `/api/medical-records/patients/{patientId}`

| Метод | Путь | Scope | Описание |
|-------|------|-------|----------|
| POST | `/events` | write:events | Добавить событие (идемпотентно по `eventId`) |
| GET | `/history` | read:history | Хронология + текущее состояние |
| GET | `/state` | read:projections | Только проекции (быстро) |
| POST | `/access-grants` | manage:consents | Выдать доступ |
| DELETE | `/access-grants/{id}` | manage:consents | Отозвать доступ |

Внутренние вызовы сервисов: `/internal/medical-records/patients/{patientId}/...`  
Заголовки (dev): `X-Service-Name`, `X-User-Id`, `X-User-Roles`.

## Примеры payload

**DiagnosisConfirmed**
```json
{
  "icd10Code": "J20.9",
  "description": "Острый бронхит неуточненный"
}
```

**LabResultReceived**
```json
{
  "testName": "CRP",
  "resultValue": "45 mg/L",
  "referenceRange": "<5",
  "isCritical": true
}
```

## Kafka

Секция `Kafka` в конфигурации. Сейчас публикация — заглушка (`LoggingMedicalRecordEventPublisher`). При `Enabled: true` подключите Confluent producer к топику `medical-record.events`.

## Запуск

```bash
cd src/Services/MedicalRecordService
docker compose up -d postgres-medical
dotnet run --project MedicalRecordService.API
```

Swagger: http://localhost:5210/swagger  
PostgreSQL: порт **5434**, БД `MedicalRecordDb`.

## Доступ

- Пациент читает/управляет согласиями по своему `patientId` (= `sub` в JWT).
- Врач — при активном `access_grants` с нужным scope.
- Системные сервисы — роль `Service` / `System` или заголовок `X-Service-Name` на internal API.
