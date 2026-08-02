# AI Triage Service

Интеллектуальное ядро Vitals: разбор симптомов из чата, оценка срочности, формирование данных для маршрутизации. **Не ставит диагноз** и **не назначает лекарства**.

## Компоненты (текущая версия)


| Компонент        | Реализация                                            |
| ---------------- | ----------------------------------------------------- |
| Парсер симптомов | Rule-based (`RuleBasedSymptomParser`)                 |
| NER              | **Заглушка** (`StubNerService`) + коды МКБ-10         |
| LLM              | **YandexGPT 5.1** (`YandexGptLlmTriageService`)       |
| Medical Record   | HTTP → `internal/medical-records/patients/{id}/state` |
| Kafka / HTTP     | `triage.completed` или sync routing HTTP              |

Ключ API: `appsettings.Secrets.json` / env `MlServices__ApiKey` / `.env` → `YANDEX_API_KEY` (**не коммитить**).


## API


| Метод | Путь                                 | Описание                              |
| ----- | ------------------------------------ | ------------------------------------- |
| POST  | `/api/triage/sessions`               | Создать сессию триажа                 |
| POST  | `/api/triage/sessions/{id}/messages` | Сообщение пациента → ответ ассистента |
| POST  | `/api/triage/sessions/{id}/complete` | **Завершить триаж** → YandexGPT (если нужно) + routing |
| GET   | `/api/triage/sessions/{id}`          | Сессия + последняя оценка (patient self / doctor with access) |
| GET   | `/api/triage/patients/{patientId}/sessions?limit=5` | Список сессий пациента для врача (403 без grant/консультации) |


Требуется JWT (Bearer). Internal (по `X-Service-Key`):
`GET /internal/triage/sessions/{id}`, `GET /internal/triage/patients/{patientId}/sessions?limit=1`
(второй использует Gateway для календаря врача, когда у консультации нет `triageSessionId`).

Gateway: `GET /api/v1/triage/patients/{patientId}/sessions`.

## Поток обработки сообщения

1. Rule-based парсер извлекает симптомы, локализацию, длительность и т.д.
2. NER-заглушка нормализует термины (МКБ-10).
3. Загружается контекст из Medical Record Service.
4. YandexGPT возвращает JSON: гипотезы, urgency (1–5), следующий вопрос, рекомендацию.
5. Ответ пациенту с disclaimer (routing — на `complete`).

## Завершение триажа

`POST /api/v1/triage/sessions/{sessionId}/complete`

1. Берётся последняя оценка YandexGPT из диалога; если сообщений не было — финальный вызов модели по истории.
2. Публикация / HTTP в Routing → `active-route`, `routingDecisionId`, `assignedDoctorId` в ответе.
3. Событие в медкарту.

Mock-оценка больше не используется при `MlServices:UseStubModels=false`.

`patientId` в сессии триажа — id пациента из JWT (тот же, что на фронте в `useAuth().patientId`).

## Пример

Запрос:

```json
{ "message": "У меня уже третий день болит голова, особенно в висках, боль пульсирующая, таблетки не помогают" }
```

Извлекается: головная боль, виски, 3 дня, пульсирующая, не помогает терапия.

## Запуск

```bash
cd src/Services/AITriageService
docker compose up -d postgres-triage
dotnet run --project AITriageService.API
```

Swagger: [http://localhost:5220/swagger](http://localhost:5220/swagger)  
PostgreSQL: порт **5435**

## Безопасность

- При экстренных формулировках (боль в груди, одышка, «вызовите скорую») urgency = **5** + явное предупреждение.
- В каждом ответе: это предварительная оценка, не диагноз.

## Дальше

- Подключить реальные NER/LLM через `MlServices:NerEndpoint` / `LlmEndpoint`
- Confluent producer для Kafka
- ~~Маршрут в ApiGateway~~ — настроено: `/api/v1/triage/*`
