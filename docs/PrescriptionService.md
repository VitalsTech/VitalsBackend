# Prescription Service

Центральное звено назначений Vitals: создание рецептов, проверки безопасности, подпись, отправка в аптеку, QR-код, инструкции для пациента; направления на лабораторные анализы.

## Компоненты (текущая версия)

| Компонент | Реализация |
| --- | --- |
| Проверки | Rule-based (`RuleBasedPrescriptionValidationEngine`) |
| Аллергии / взаимодействия | Данные из Medical Record + hardcoded правила |
| Права врача | **Заглушка** (`StubUserPermissionClient`) |
| Электронная подпись | **Заглушка** (`StubESignatureService`) |
| Аптека | **Заглушка** (`StubPharmacyIntegrationClient`) |
| Kafka | **Заглушка** (`LoggingPrescriptionEventPublisher`) |
| QR-код | `QRCoder` + HMAC-подпись payload |
| Инструкции | Шаблонный генератор |
| Напоминания | `PrescriptionExpiryHostedService` (8:00 UTC scan) |

## Статусы рецептов

`Draft` → `Signed` → `SentToPharmacy` → `Fulfilled` / `PartiallyFulfilled` / `Expired` / `Cancelled`

## Статусы направлений на анализы

`Ordered` → `InProgress` → `Completed` / `Cancelled`

## Результаты проверки рецепта

`Allowed` | `RequiresConfirmation` | `Blocked`

## API — рецепты (JWT)

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/api/prescriptions` | Создать черновик (+ валидация) |
| GET | `/api/prescriptions/{id}` | Рецепт |
| GET | `/api/prescriptions/patients/{id}` | Рецепты пациента (не Draft) |
| POST | `/api/prescriptions/{id}/validate` | Повторная проверка |
| POST | `/api/prescriptions/{id}/sign?confirmWarnings=` | Подписать |
| POST | `/api/prescriptions/{id}/send-to-pharmacy` | Отправить в аптеку |
| POST | `/api/prescriptions/{id}/cancel` | Отменить |
| GET | `/api/prescriptions/{id}/qr` | QR (PNG base64) |
| GET | `/api/prescriptions/{id}/instructions` | Инструкция для пациента |

## API — анализы (JWT)

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/api/lab-orders` | Создать направление (`Ordered`) — Doctor |
| GET | `/api/lab-orders/{id}` | Направление с позициями и результатами |
| GET | `/api/lab-orders/patients/{id}` | Все направления пациента |
| POST | `/api/lab-orders/{id}/start` | Взять в работу (`InProgress`) — Doctor |
| POST | `/api/lab-orders/{id}/cancel` | Отменить — Doctor |
| POST | `/api/lab-orders/{id}/results` | Записать результаты (`Completed`) — Doctor |

Internal:

| Метод | Путь | Описание |
| --- | --- | --- |
| POST | `/internal/prescriptions/{id}/fulfillment` | Подтверждение выдачи из аптеки |
| POST | `/internal/lab-orders/{id}/results` | Результаты от внешней лаборатории |

## Пример

```json
POST /api/prescriptions
{
  "patientId": "...",
  "consultationId": "...",
  "diagnosisForPrescription": "J06.9 ОРВИ",
  "confirmWarnings": false,
  "medications": [{
    "tradeName": "Амоксициллин",
    "inn": "amoxicillin",
    "dosageForm": "tablets",
    "dosage": "500 mg",
    "packageQuantity": "20",
    "route": "oral",
    "frequency": "3 times daily",
    "courseDays": 7,
    "atcCode": "J01CA04",
    "requiresPrescription": true
  }]
}
```

## Запуск

```bash
cd src/Services/PrescriptionService
docker compose up -d postgres-prescription
dotnet run --project PrescriptionService.API
```

Swagger: [http://localhost:5250/swagger](http://localhost:5250/swagger)  
PostgreSQL: **5438**  
Gateway: `/api/v1/prescriptions/*`, `/api/v1/lab-orders/*`

## Пример — направление на анализы

```json
POST /api/lab-orders
{
  "patientId": "...",
  "consultationId": "...",
  "clinicalIndication": "контроль воспаления",
  "priority": "routine",
  "items": [
    { "testName": "ОАК", "testCode": "CBC", "specimenType": "blood" },
    { "testName": "СРБ", "testCode": "CRP" }
  ]
}
```

Результаты:

```json
POST /api/lab-orders/{id}/results
{
  "markCompleted": true,
  "results": [{
    "itemId": "...",
    "resultValue": "12.5",
    "unit": "mg/L",
    "referenceRange": "< 5",
    "isCritical": true
  }]
}
```

## Kafka-события (лог)

- `prescription.created`
- `prescription.status_updated`
- `prescription.expiring_soon`
- `prescription.expired`
- `prescription.fulfilled`
