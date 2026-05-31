# UserService

`UserService` отвечает за регистрацию пользователей, управление профилями, ролями и правами доступа, а также за административные операции над учетными записями.

## Назначение

Сервис хранит:

- базовую карточку пользователя;
- один или несколько профилей пользователя;
- роли и права, назначенные на профиль;
- административные атрибуты состояния пользователя, включая блокировку и soft delete.

Сервис разделен на слои:

- `API` содержит контроллеры, валидаторы и Swagger-настройку;
- `Application` содержит бизнес-логику, DTO и маппинг;
- `Domain` содержит сущности и перечисления;
- `Infrastructure` содержит `DbContext` и репозитории.

## Основные принципы

- `User` является корневой сущностью.
- У пользователя может быть несколько профилей.
- Профиль принадлежит только одному типу: `Patient`, `Doctor` или `Organization`.
- Роли назначаются на конкретный профиль, а не только на пользователя.
- Права вычисляются через роли профиля.
- Swagger строится на DTO и FluentValidation-правилах, а не на ручном дублировании контракта.

## Конфигурация API

Точка входа:

- `src/Services/UserService/UserService.API/Program.cs`

Регистрация Swagger и FluentValidation:

- `Swashbuckle.AspNetCore`
- `FluentValidation.AspNetCore`
- `MicroElements.Swashbuckle.FluentValidation`

Что это дает:

- ограничения из валидаторов попадают в OpenAPI;
- для request DTO в Swagger видны `required`, `pattern`, `maxLength`, `minimum`, `maximum`;
- для enum-полей видны допустимые значения;
- для основных запросов добавлены примеры JSON.

## Схема данных

### User

Поля:

- `Id`
- `PublicId`
- `PhoneNumber`
- `Email`
- `FirstName`
- `SecondName`
- `Surename`
- `BirthDate`
- `Sex`
- `CreatedAt`
- `UpdatedAt`
- `IsActive`
- `BlockedUntil`
- `BlockReason`

Смысл полей:

- `PublicId` используется во внешнем API.
- `Id` используется как внутренний ключ.
- `IsActive = false` означает, что пользователь неактивен.
- `BlockedUntil`, если задано в будущем, означает временную блокировку.
- `BlockReason` хранит причину блокировки.

### Profile

Поля:

- `Id`
- `UserId`
- `ProfileType`
- `IsActive`
- `CreatedAt`

Типы профиля:

- `Patient`
- `Doctor`
- `Organization`

### DoctorProfile

Поля:

- `Specialization`
- `DiplomaNumber`
- `DiplomaSeries`
- `CertificateNumber`
- `CertificateExpiryDate`
- `OrganizationId`
- `Category`
- `AcademicDegree`
- `Biography`
- `Rating`
- `ReviewCount`

### PatientProfile

Поля:

- `InsuranceNumber`
- `SNILS`
- `BloodType`
- `Allergies`

### OrganizationProfile

Поля:

- `LegalName`
- `DisplayName`
- `INN`
- `KPP`
- `OGRN`
- `LegalAddress`
- `ContactPhone`
- `ContactEmail`
- `Role`
- `AdministratorId`

## Request DTO

### CreateUserWithProfileRequest

Обязательные поля:

- `PhoneNumber`
- `FirstName`
- `Surename`
- `BirthDate`
- `Sex`

Правила:

- `PhoneNumber` должен содержать 10-15 цифр;
- `FirstName` и `Surename` ограничены 100 символами;
- `BirthDate` не может быть в будущем и не может быть старше 120 лет;
- `Sex` принимает только `Male` или `Female`;
- должен быть передан ровно один профиль:
  - `PatientProfile`;
  - `DoctorProfile`;
  - `OrganizationProfile`.

### AddProfileToExistingUserRequest

Обязательные поля:

- `UserPublicId`
- `ProfileType`

Правила:

- `ProfileType` принимает только `Patient`, `Doctor`, `Organization`;
- должен быть передан ровно один объект профиля;
- тип переданного объекта профиля должен совпадать с `ProfileType`.

### CreatePatientProfileRequest

Поля:

- `InsuranceNumber`
- `SNILS`
- `BloodType`
- `Allergies`

Правила:

- `InsuranceNumber` ограничен 20 символами;
- `SNILS` должен содержать 11 цифр;
- `BloodType` принимает только значения:
  - `APositive`
  - `ANegative`
  - `BPositive`
  - `BNegative`
  - `ABPositive`
  - `ABNegative`
  - `OPositive`
  - `ONegative`

### CreateDoctorProfileRequest

Обязательные поля:

- `Specialization`
- `DiplomaNumber`
- `CertificateNumber`
- `CertificateExpiryDate`
- `Category`

Правила:

- `Specialization` ограничена 100 символами;
- `DiplomaNumber` ограничен 50 символами;
- `DiplomaSeries` необязательна;
- `CertificateNumber` ограничен 50 символами;
- `CertificateExpiryDate` должна быть в будущем;
- `Category` принимает только:
  - `None`
  - `Second`
  - `First`
  - `Highest`

### CreateOrganizationProfileRequest

Обязательные поля:

- `LegalName`
- `DisplayName`
- `INN`
- `OGRN`
- `Role`

Правила:

- `LegalName` ограничено 200 символами;
- `DisplayName` ограничено 100 символами;
- `INN` должен содержать 10 или 12 цифр;
- `OGRN` должен содержать 13 или 15 цифр;
- `Role` принимает только:
  - `Clinic`
  - `Laboratory`
  - `Pharmacy`

### UserSearchRequest

Обязательные поля:

- нет обязательных фильтров, но `Page` и `PageSize` имеют значения по умолчанию.

Правила:

- `Page` начинается с `0`;
- `PageSize` должен быть от `1` до `100`.

Поисковые фильтры:

- `PhoneNumber`
- `Email`
- `FirstName`
- `Surename`
- `UserType`
- `Specialization`
- `OrganizationRole`

### UserBlockRequest

Поля:

- `BlockedUntil`
- `BlockReason`

Правила:

- если `BlockedUntil` задан, дата должна быть в будущем;
- если `BlockedUntil` не задан, блокировка считается бессрочной через `IsActive = false`.

### UserStatusRequest

Поля:

- `IsActive`
- `Reason`

Правила:

- `IsActive` обязательное;
- `Reason` необязательное.

### SwitchActiveProfileRequest

Обязательные поля:

- `UserPublicId`
- `ProfileId`

### PermissionCheckRequest

Обязательные поля:

- `UserPublicId`
- `Permission`

## Response DTO

### UserWithProfilesDto

Содержит:

- `PublicId`
- `PhoneNumber`
- `Email`
- `FirstName`
- `SecondName`
- `Surename`
- `BirthDate`
- `Sex`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `Profiles`
- `ActiveProfileId`

### ProfileInfoDto

Содержит:

- `ProfileId`
- `ProfileType`
- `IsActive`
- `Data`

`Data` является полиморфным объектом и зависит от `ProfileType`.

### ActiveProfileResponse

Содержит:

- `ProfileId`
- `ProfileType`
- `ProfileData`

### UserDto

Используется для административного поиска и содержит:

- `PublicId`
- `Email`
- `PhoneNumber`
- `FirstName`
- `SecondName`
- `Surename`
- `BirthDate`
- `Sex`
- `InsuranceNumber`
- `SNILS`
- `RegistrationAddress`
- `ActualAddress`
- `CreatedAt`
- `UpdatedAt`
- `UserType`
- `IsActive`
- `BlockedUntil`
- `BlockReason`

### SearchResult<T>

Используется для ответов поиска.

Поля:

- `Items`
- `TotalCount`
- `Page`
- `PageSize`
- `TotalPages`

## Endpoint-ы

### Users

`POST /api/users/register`

Создает пользователя с одним профилем.

`GET /api/users/{publicId}`

Возвращает пользователя со всеми профилями.

`GET /api/users/{publicId}/profiles`

Возвращает список профилей пользователя.

`POST /api/users/add-profile`

Добавляет профиль существующему пользователю.

`POST /api/users/switch-profile`

Делает указанный профиль активным.

`GET /api/users/{publicId}/has-profile/{profileType}`

Проверяет наличие профиля указанного типа.

### Admin

`POST /api/admin/users/search`

Административный поиск пользователей с пагинацией.

`PUT /api/admin/users/{publicId}/status`

Меняет активность пользователя.

`POST /api/admin/users/{publicId}/block`

Блокирует пользователя.

`POST /api/admin/users/{publicId}/unblock`

Снимает блокировку.

`DELETE /api/admin/users/{publicId}/soft`

Мягко удаляет пользователя.

`POST /api/admin/users/{publicId}/restore`

Восстанавливает пользователя.

`POST /api/admin/permissions/check`

Проверяет наличие права.

`GET /api/admin/permissions/user/{publicId}`

Возвращает роли и права пользователя.

### Roles

`GET /api/admin/roles`

Возвращает список ролей.

`GET /api/admin/roles/permissions`

Возвращает список прав.

`POST /api/admin/roles/users/{publicId}/assign`

Назначает роль профилю пользователя.

`DELETE /api/admin/roles/users/{publicId}/unassign`

Снимает роль с профиля пользователя.

`GET /api/admin/roles/users/{publicId}`

Возвращает роли и права по профилям пользователя.

## Ошибки и статусы

Типовые ответы:

- `400 Bad Request` для ошибок валидации и неверных значений enum;
- `404 Not Found` если пользователь или профиль не найдены;
- `409 Conflict` если профиль или роль уже назначены;
- `500 Internal Server Error` только для неожиданных ошибок.

Глобальная обработка исключений находится в:

- `src/Services/UserService/UserService.API/Middleware/GlobalExceptionHandler.cs`

## Пагинация

В административном поиске используется zero-based пагинация:

- `Page = 0` — первая страница;
- `PageSize` — размер страницы.

Расчет:

- `Skip(Page * PageSize)`

## Блокировка пользователя

Блокировка учитывается через:

- `User.IsActive`
- `User.BlockedUntil`

Правило доступа:

- если `IsActive = false`, пользователь считается неактивным;
- если `BlockedUntil` в будущем, пользователь считается заблокированным;
- права и роли не должны возвращаться как доступные для заблокированного пользователя.

## Swagger

Swagger собирается так, чтобы схема была пригодна для ручной сборки запроса с нуля:

- из FluentValidation подтягиваются ограничения;
- для enum показываются допустимые значения;
- для основных запросов добавлены примеры;
- для сложных полей оставлены описания в schema filter.

Файлы Swagger-настройки:

- `src/Services/UserService/UserService.API/Program.cs`
- `src/Services/UserService/UserService.API/Swagger/ValidationSchemaFilter.cs`
- `src/Services/UserService/UserService.API/Swagger/RequestExampleOperationFilter.cs`
