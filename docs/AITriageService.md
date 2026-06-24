# AI Triage Service

Интеллектуальное ядро Vitals: разбор симптомов из чата, оценка срочности, формирование данных для маршрутизации. **Не ставит диагноз** и **не назначает лекарства**.

## Компоненты (текущая версия)


| Компонент        | Реализация                                            |
| ---------------- | ----------------------------------------------------- |
| Парсер симптомов | Rule-based (`RuleBasedSymptomParser`)                 |
| NER              | **Заглушка** (`StubNerService`) + коды МКБ-10         |
| LLM              | **Заглушка** (`StubLlmTriageService`)                 |
| Medical Record   | HTTP → `internal/medical-records/patients/{id}/state` |
| Kafka            | **Заглушка** → топик `triage.completed`               |


## API


| Метод | Путь                                 | Описание                              |
| ----- | ------------------------------------ | ------------------------------------- |
| POST  | `/api/triage/sessions`               | Создать сессию триажа                 |
| POST  | `/api/triage/sessions/{id}/messages` | Сообщение пациента → ответ ассистента |
| GET   | `/api/triage/sessions/{id}`          | Сессия + последняя оценка             |


Требуется JWT (Bearer). Internal: `GET /internal/triage/sessions/{id}`.

## Поток обработки сообщения

1. Rule-based парсер извлекает симптомы, локализацию, длительность и т.д.
2. NER-заглушка нормализует термины (МКБ-10).
3. Загружается контекст из Medical Record Service.
4. LLM-заглушка возвращает гипотезы, urgency (1–5), следующий вопрос, рекомендацию.
5. Публикуется `triage.completed` (лог).
6. Ответ пациенту с disclaimer.

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
