# Prescription Service

Центральное звено назначений Vitals: создание рецептов, проверки безопасности, подпись, отправка в аптеку, QR-код, инструкции для пациента.

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

## Статусы

`Draft` → `Signed` → `SentToPharmacy` → `Fulfilled` / `PartiallyFulfilled` / `Expired` / `Cancelled`

## Результаты проверки

`Allowed` | `RequiresConfirmation` | `Blocked`

## API (JWT)

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

Internal:

| POST | `/internal/prescriptions/{id}/fulfillment` | Подтверждение выдачи из аптеки |

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
Gateway: `/api/v1/prescriptions/*`

## Kafka-события (лог)

- `prescription.created`
- `prescription.status_updated`
- `prescription.expiring_soon`
- `prescription.expired`
- `prescription.fulfilled`
