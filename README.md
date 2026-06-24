# VitalsBackend

| Сервис | Путь | Порт (dev) |
|--------|------|------------|
| **ApiGateway** | `src/Services/ApiGateway` | **5080** (единая точка входа) |
| UserService | `src/Services/UserService` | 5195 |
| AuthService | `src/Services/AuthService` | 5200 |
| MedicalRecordService | `src/Services/MedicalRecordService` | 5210 |

Клиенты обращаются только к **Gateway** (`/api/v1/...`).

Документация: [ApiGateway](docs/ApiGateway.md), [UserService](docs/UserService.md), [AuthService](docs/AuthService.md), [MedicalRecordService](docs/MedicalRecordService.md).
