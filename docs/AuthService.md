# AuthService

`AuthService` отвечает только за **аутентификацию**: проверку учётных данных и выдачу токенов. Профили и RBAC остаются в `UserService`.

## База данных (изолированная)


| Таблица                 | Назначение                                             |
| ----------------------- | ------------------------------------------------------ |
| `auth_users`            | `UserPublicId`, телефон, хэш пароля + соль, блокировка |
| `refresh_tokens`        | UUID refresh-токен, fingerprint, IP, срок, отзыв       |
| `esia_links`            | связь пользователя с идентификатором ЕСИА              |
| `password_reset_tokens` | коды восстановления пароля                             |


Пароли: **Argon2id** (соль отдельным полем, сравнение через constant-time).

## Токены


| Тип     | Формат    | TTL     |
| ------- | --------- | ------- |
| Access  | JWT RS256 | 15 мин  |
| Refresh | UUID в БД | 30 дней |


Claims access-токена: `sub` (PublicId), `role`, `scope` (права из UserService). Без ФИО/телефона.

## Публичные эндпоинты (`/api/auth`)


| Метод | Путь               | Описание                                           |
| ----- | ------------------ | -------------------------------------------------- |
| POST  | `/register`        | Регистрация → UserService + учётка в Auth DB       |
| POST  | `/login`           | Телефон + пароль → пара токенов                    |
| POST  | `/refresh`         | Новая пара по refresh                              |
| POST  | `/logout`          | Отзыв refresh                                      |
| POST  | `/change-password` | JWT + старый/новый пароль                          |
| POST  | `/password/forgot` | Код сброса (пока логируется, SMS/email — заглушка) |
| POST  | `/password/reset`  | Сброс по коду                                      |
| GET   | `/esia/config`     | Включена ли ЕСИА, портал, что импортируется        |
| GET   | `/esia/start`      | URL авторизации (`intent=register\|link`)          |
| GET   | `/esia/login`      | Алиас `start?intent=register`                      |
| GET   | `/esia/callback`   | Callback ЕСИА → JWT или редирект на returnUrl      |
| GET   | `/esia/status`     | JWT: привязан ли аккаунт                           |
| POST  | `/esia/complete`   | Обмен code+state (если клиент сам поймал code)     |
| POST  | `/esia/link`       | Привязка по code (JWT; пароль необязателен)        |


## Внутренние эндпоинты (`/internal`)


| Метод | Путь              | Описание                    |
| ----- | ----------------- | --------------------------- |
| POST  | `/token/validate` | Проверка JWT                |
| GET   | `/jwks`           | Публичный ключ RS256 (JWKS) |


## Интеграция с UserService

HTTP-клиент (`UserService:BaseUrl`):

- `POST /api/users/register`
- `GET /internal/users/by-phone/{phone}`
- `GET /internal/users/{publicId}/roles-permissions`

## Запуск

```bash
# PostgreSQL Auth (порт 5433)
cd src/Services/AuthService
docker compose up -d postgres-auth

# UserService должен быть запущен на :5195
cd ../UserService
dotnet run --project UserService.API

# AuthService
cd ../AuthService
dotnet run --project AuthService.API
```

Swagger: [http://localhost:5200/swagger](http://localhost:5200/swagger)

## Конфигурация

- `Jwt` — issuer, audience, RSA PEM (в dev ключ генерируется при старте, если PEM не задан)
- `Esia:Enabled` — в `appsettings.json` `false` (prod). На **DEV** (`UseStub: true`) живого портала нет: форма ФИО/почта/телефон, ОМС и адрес генерируются, аккаунты склеиваются по телефону. Production не включится без `Esia:AllowProduction`. Клиники/врачи — нет.

