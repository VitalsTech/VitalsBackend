# VitalsBackend

| Сервис | Путь | Порт (dev) |
|--------|------|------------|
| **ApiGateway** | `src/Services/ApiGateway` | **5080** |
| UserService | `src/Services/UserService` | 5195 |
| AuthService | `src/Services/AuthService` | 5200 |
| MedicalRecordService | `src/Services/MedicalRecordService` | 5210 |
| AITriageService | `src/Services/AITriageService` | 5220 |
| **RoutingService** | `src/Services/RoutingService` | **5230** |
| **ConsultationService** | `src/Services/ConsultationService` | **5240** |

Клиенты обращаются к **Gateway** (`/api/v1/...`).

Документация: [ApiGateway](docs/ApiGateway.md), [UserService](docs/UserService.md), [AuthService](docs/AuthService.md), [MedicalRecordService](docs/MedicalRecordService.md), [AITriageService](docs/AITriageService.md), [RoutingService](docs/RoutingService.md), [ConsultationService](docs/ConsultationService.md), [**Заглушки**](docs/Stubs.md).
