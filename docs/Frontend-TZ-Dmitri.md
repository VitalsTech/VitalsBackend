# ТЗ для фронтенда (VitalsWeb)

Задачи из списка Dmitri, которые **не входят в бэкенд** или требуют доработки UI поверх уже готовых API.

---

## 1. «О враче» — пустой блок (скриншот)

**Бэкенд (готово):** `PATCH /api/v1/doctors/me/profile` — поля `biography`, `specialization`, `academicDegree`.  
`GET /api/v1/doctors/{id}` возвращает `profiles[].data.biography`.

**Фронтенд:**
- На странице профиля врача (`DoctorProfile`) — форма редактирования «О враче» (textarea), сохранение через PATCH.
- На карточке пациента (`DoctorDetail`, сайдбар чата) — показывать `bio` / `biography`; если пусто — placeholder «Информация уточняется», но блок не скрывать.
- При регистрации врача — опциональное поле «О себе» → `doctorProfile.biography`.
- Не скрывать секцию «О враче» при `biography === null`.

---

## 2. Уведомления

**Бэкенд (готово):** mood/triage pipeline, категории `mood`/`triage` в preferences, поле `category` в history.

**Фронтенд:**
- Фильтры на странице уведомлений врача: `mood`, `triage`, `messages`, … — по полю `category`.
- Маппинг `title`/`message` из `subject`/`body` (уже частично есть).
- Deep link `/doctor/patients/{patientId}` из payload / текста.
- Badge непрочитанных — **пока нет API read/unread**; временно считать все доставленные как новые или скрыть badge до появления `PATCH .../read`.

---

## 3. Консультации и направления (анализы, маршрут)

**Бэкенд (готово):**  
- `GET /api/v1/routing/decisions/{id}`  
- `GET /api/v1/routing/patients/{patientId}/active-route`  
- Complete consultation с `labOrders[]` в протоколе.

**Фронтенд:**
- Экран «Мой путь» / карточка пациента — читать `active-route` и показывать шаги (анализы → консультация → …).
- При завершении консультации врачом — UI для `labOrders` (список направлений на анализы).
- Связать triage → routing → consultation в UX (не только medical history).
- Отображать `recommendedLabs` из routing decision.

---

## 4. Рецепты в документы

**Бэкенд (готово):** при подписании рецепта — события `PrescriptionIssued` + `DocumentUploaded` в медкарте; `GET .../prescriptions/{id}/instructions`.

**Фронтенд:**
- Раздел «Документы» пациента/врача — фильтр history/attachments: `DocumentUploaded`, `PrescriptionIssued`.
- Карточка документа «Рецепт» со ссылкой на `GET /api/v1/prescriptions/{id}/instructions`.
- `GET /api/v1/medical-records/patients/{id}/attachments` — список вложений (gateway добавлен).

---

## 5. Сообщения чата — цвета, время, прочитано

**Бэкенд (готово):**
- `GET /api/v1/consultations/mine?includeCompleted=false&limit=50` — список консультаций текущего
  пользователя. У пациента здесь и свободный чат с врачом (`isScheduled: false`), и записи через
  `book` (`isScheduled: true`, есть `scheduledAt` / `scheduledSlotId`).
- Вход в конкретную консультацию: `GET /api/v1/consultations/{sessionId}`, затем
  `POST .../join`, `GET .../messages` — **уже работало**; не хватало только списка, из которого
  фронт берёт `sessionId`.
- `GET /api/v1/consultations/active?patientId=&doctorId=` — один «свободный» активный чат пары
  (не замена списку записей).
- `sentAt`, `readAt` в DTO сообщений.
- `GET .../messages?markAsRead=true` — помечает входящие прочитанными.  
- `POST .../messages/read` — явная отметка.  
- SignalR event `messagesRead`.

**Фронтенд:**
- Экран пациента «Мои консультации» / раздел в чатах: `GET /api/v1/consultations/mine`.
  Разделять: запланированные (`isScheduled === true`, показать `scheduledAt`) и обычные чаты.
  Клик → маршрут на `/consultations/{sessionId}` (join + messages), **не** на `active` с врачом.
- После `book` — сохранить `sessionId` из ответа и сразу открыть/показать карточку записи.
- Свои сообщения — один стиль (например справа, акцентный фон); чужие — другой (слева, нейтральный).
- Показывать время `sentAt` (локаль пользователя).
- Статус «прочитано» — если `readAt != null` у исходящих; подпись «доставлено» / «прочитано».
- Подписка на `messagesRead` в SignalR hub — обновлять `readAt` у сообщений собеседника.
- При открытии чата — polling/`GET messages?markAsRead=true`.

---

## 6. Календарь врача

**Бэкенд (готово):**  
- `POST /api/v1/consultations/book { doctorId, slotId, consultationType, urgencyLevel }` — запись пациента
  на слот: резервирует слот и создаёт консультацию на его время, возвращает `sessionId` и время приёма.
  409 — слот успели занять (нужно перезагрузить расписание), 404 — слот не найден.
  **Важно:** старый `POST /api/v1/consultations` слот не занимает и создаёт консультацию «на сейчас» —
  для записи по расписанию использовать только `book`.
- `GET /api/v1/doctors/me/calendar?from=&days=` — календарь с деталями занятости: `isBooked`, `status`,
  пациент (`fullName`, `age`, `sex`), `triage` (уровень срочности, жалобы, гипотезы, рекомендация),
  `anamnesis` (диагнозы, препараты, аллергии, анализы, последний показатель).
  Консультации вне сетки приёма — в `unscheduledConsultations`. Контракт: `docs/ApiGateway.md`.
- `GET /api/v1/doctors/{id}/schedule` — публичное расписание для записи пациента (без данных пациентов)
- `POST /api/v1/doctors/me/schedule/slots` — создать/обновить слот (забронированный слот менять нельзя — 400)  
- `DELETE /api/v1/doctors/me/schedule/slots/{slotId}` (забронированный слот удалить нельзя — 400)

**Фронтенд:**
- Страница календаря врача: недельный/дневной вид слотов на основе `me/calendar`.
- Форма: дата, время начала/конца, онлайн/очно, доступен/заблокирован.
- CRUD через новые endpoints. Время слота отправлять в UTC с суффиксом `Z`.
- Цвет ячейки по `status`: `booked` / `available` / `closed`; бейдж срочности по `triage.urgencyLabel`.
- Клик по занятой ячейке — панель деталей: пациент, жалобы, гипотезы, анамнез, кнопка перехода в чат
  консультации по `consultation.sessionId`. Поля `triage`/`anamnesis` могут быть `null` — скрывать блок.
- Секция «Вне расписания» для `unscheduledConsultations` — это незакрытые консультации без брони
  (созданы триажем или записаны до появления бронирования).
- Экран записи пациента: слоты из `GET /api/v1/doctors/{id}/schedule` c `isAvailable: true`,
  запись через `POST /api/v1/consultations/book`, на 409 — перезагрузить расписание и показать
  «слот только что заняли».

---

## 7. Ошибки на русском

**Бэкенд (готово):** Gateway локализует типовые ошибки 4xx/5xx в JSON `{ error, errors }`.

**Фронтенд:**
- Показывать пользователю `error` / первое сообщение из `errors`, не raw English stack.
- Toast/alert с текстом из ответа API.
- Fallback: «Не удалось выполнить операцию».

---

## 8. Favicon и title страниц

**Только фронтенд:**
- `index.html`: `<link rel="icon" …>`, default `<title>Vitals</title>`.
- React Router / layout: `document.title` per route, например «Консультации · Vitals», «Пациент Анна · Vitals».

---

## 9. Логотип на страницах

**Только фронтенд:**
- Компонент `Logo` в header/sidebar (patient + doctor layouts).
- Единый asset в `public/` или `src/assets`.
- Согласовать с брендом Vitals (из Figma при наличии).

---

## 10. Дополнить Figma

**Дизайн / фронт:**
- Экраны: уведомления (фильтры mood/triage), календарь врача, документы с рецептами, чат (read receipts).
- Пустые состояния: «О враче», нет уведомлений, нет документов.
- Передать ссылки на макеты в README портала.

---

## Приоритет внедрения (фронт)

| P | Задача | Зависимость от API |
|---|--------|-------------------|
| 1 | «О враче» — форма + отображение | PATCH profile |
| 1 | Чат — цвета, время, readAt | messages + SignalR |
| 2 | Уведомления — фильтр category | history.category |
| 2 | Документы — рецепты из history | PrescriptionIssued / DocumentUploaded |
| 3 | Маршрут / анализы | routing active-route |
| 3 | Календарь врача | schedule CRUD |
| 4 | Favicon, title, logo | — |
| 4 | Figma | — |

---

## Контракты для проверки (curl через gateway)

```bash
# Биография врача
curl -X PATCH -H "Authorization: Bearer $DOCTOR" -H "Content-Type: application/json" \
  "$GATEWAY/api/v1/doctors/me/profile" -d '{"biography":"Опыт 10 лет…"}'

# Прочитать сообщения
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "$GATEWAY/api/v1/consultations/$SESSION/messages/read"

# Маршрут пациента
curl -H "Authorization: Bearer $TOKEN" \
  "$GATEWAY/api/v1/routing/patients/$PATIENT_ID/active-route"

# Документы
curl -H "Authorization: Bearer $TOKEN" \
  "$GATEWAY/api/v1/medical-records/patients/$PATIENT_ID/attachments"
```
