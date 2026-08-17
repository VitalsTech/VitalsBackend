# ТЗ фронтенда — видео-консультации

Цель: экран **видеозвонка внутри консультации**, где врач и пациент одновременно видят друг друга, пишут в **тот же чат сессии** и врач оформляет **диагноз / рецепт / справку**, не уходя со звонка.

Базовый URL: `GATEWAY` (локально `http://localhost:5080`).  
JWT: `Authorization: Bearer …`. Роль: `Patient` / `Doctor`.

Связанные документы: `docs/ConsultationService.md`, `docs/Frontend-TZ-Dmitri.md` (чат, join, complete).

---

## 0. Критерий готовности

Пациент и врач:

1. Входят в одну консультацию (`join` + SignalR `JoinSession`).
2. Запускают видео (камера + микрофон) **не покидая** экран консультации.
3. Пишут в чат этой консультации, пока идёт звонок.
4. Завершают только видео (чат остаётся) или закрывают приём через `/complete`.

Врач дополнительно **во время звонка**:

- ставит диагноз;
- выписывает рецепт;
- выдаёт справку.

Действия сразу видны в чате (системные сообщения) и в панели клинических действий.

---

## 1. Экран: раскладка

Один экран консультации, **без навигации** на отдельную «видеостраницу».

| Зона | Кто | Содержимое |
|------|-----|------------|
| Видео (основное) | оба | remote stream (собеседник), PiP — свой preview |
| Чат | оба | история `GET .../messages` + ввод + SignalR `messageReceived` |
| Клиника | **только врач** | формы диагноза, рецепта, справки; список `GET .../clinical` |
| Управление | оба | mute audio/video, завершить видео, (врач) complete приёма |

Если видео ещё не начато — в зоне видео кнопка «Начать видео» / «Присоединиться». Чат уже работает.

`GET /api/v1/consultations/{sessionId}`: поле `videoActive` — идёт ли звонок; `videoRoomId` — id комнаты.

---

## 2. Режимы видео (`mode`)

Ответ `POST .../video/start` и `GET .../video`:

```json
{
  "mode": "p2p",
  "roomId": "room-<sessionId без дефисов>",
  "serverUrl": "",
  "accessToken": "...",
  "role": "Doctor",
  "chatAvailable": true,
  "signalingHub": "/api/v1/consultations/hub",
  "iceServers": [
    { "urls": "stun:stun.l.google.com:19302" },
    { "urls": "stun:stun1.l.google.com:19302" }
  ]
}
```

| `mode` | Когда | Что делать на клиенте |
|--------|--------|------------------------|
| **`p2p`** | DEV, `Sfu:UseStub: true` (по умолчанию) | Нативный **WebRTC**: `RTCPeerConnection` + ICE из `iceServers`. Сигналинг SDP/ICE — SignalR `SendRtcSignal` / событие `rtcSignal`. `serverUrl` / `accessToken` **не** подключать к LiveKit. |
| **`sfu`** | LiveKit или HTTP SFU | SDK провайдера (`livekit-client` и т.п.) с `serverUrl` + `accessToken`. Чат всё равно через тот же hub. |

Определять ветку **только по `mode`**, не по наличию токена.

---

## 3. REST (Gateway)

Префикс: `/api/v1/consultations/{sessionId}`. Все методы — JWT, участник сессии.

| Метод | Путь | Кто | Описание |
|-------|------|-----|----------|
| POST | `/video/start` | оба | Создать комнату (идемпотентно) и выдать credentials. Первый вызов шлёт `videoStarted` второй стороне. |
| GET | `/video` | оба | Credentials, если видео уже активно. Иначе **409** `{ "error": "Video is not active." }`. |
| POST | `/video/stop` | оба | Закрыть комнату. Сессия и чат **не** закрываются. **204**. |
| GET | `/messages` | оба | Чат консультации (тот же, что без видео). |
| POST | `/messages` | оба | Сообщение в чат **во время** видео. |
| GET | `/clinical` | оба | Список действий врача в этой сессии. |
| POST | `/diagnoses` | **Doctor** | Диагноз без `/complete`. |
| POST | `/prescriptions` | **Doctor** | Рецепт без `/complete`. |
| POST | `/certificates` | **Doctor** | Справка. |
| POST | `/complete` | **Doctor** | Закрыть **весь приём** (протокол). Видео лучше остановить заранее. |

Сессия не должна быть `Completed` / `Cancelled` / `Expired`.

### 3.1. Диагноз

```json
POST /api/v1/consultations/{sessionId}/diagnoses
{ "icd10": "J06.9", "text": "ОРВИ" }
```

Нужен `icd10` и/или `text`. Пишет событие медкарт `DiagnosisConfirmed`, системное сообщение в чат, `clinicalAction`.

### 3.2. Рецепт

```json
POST /api/v1/consultations/{sessionId}/prescriptions
{ "lines": ["Парацетамол 500 мг — при температуре до 3 дней"] }
```

`lines` не пустой. Формат строки как в `/complete`: `название — инструкция`. Создаётся рецепт в PrescriptionService.

### 3.3. Справка

```json
POST /api/v1/consultations/{sessionId}/certificates
{
  "type": "StudyExcuse",
  "title": "Справка в учебное заведение",
  "body": "Освобождение от занятий 17–18.08.2026.",
  "validFrom": "2026-08-17T00:00:00Z",
  "validUntil": "2026-08-18T23:59:59Z"
}
```

`type`: `HealthStatus` | `StudyExcuse` | `WorkExcuse` | `Other`.  
`title` и `body` обязательны.

### 3.4. Ответ клинического действия

```json
{
  "id": "…",
  "kind": "Diagnosis",
  "createdAt": "2026-08-17T12:00:00Z",
  "createdByDoctorId": "…",
  "payload": { "icd10": "J06.9", "text": "ОРВИ" }
}
```

`kind`: `Diagnosis` | `Prescription` | `Certificate`.  
`GET .../clinical` → `{ "items": [ … ] }`.

---

## 4. SignalR

Hub (через Gateway):

```
{GATEWAY}/api/v1/consultations/hub?access_token={JWT}
```

Пакет: `@microsoft/signalr`. Транспорт: WebSocket.

### Методы клиента → сервер

| Метод | Аргументы |
|-------|-----------|
| `JoinSession` | `sessionId` (string GUID) — **обязательно** до видео и чата |
| `LeaveSession` | `sessionId` |
| `SendRtcSignal` | `sessionId`, `signal` (см. ниже) |

`SendRtcSignal` рассылается **другим** в группе (не отправителю). Только участник сессии.

```ts
type RtcSignal = {
  type: "offer" | "answer" | "ice" | "hangup" | "media";
  sdp?: string;
  candidate?: string;
  sdpMid?: string;
  sdpMLineIndex?: number;
  audio?: boolean;
  video?: boolean;
};
```

### События сервер → клиент

| Событие | Когда | Полезные поля |
|---------|--------|----------------|
| `messageReceived` | любое сообщение чата, в т.ч. системные | как `ConsultationMessageDto` |
| `statusChanged` | смена статуса сессии | `{ sessionId, status }` |
| `messagesRead` | прочитано | `{ sessionId, readerRole, readAt, lastSequence }` |
| `videoStarted` | кто-то вызвал `POST /video/start` впервые | `{ sessionId, mode, roomId, signalingHub, chatAvailable, iceServers }` — **без** чужого `accessToken` |
| `videoStopped` | `POST /video/stop` | `{ sessionId }` |
| `rtcSignal` | SDP/ICE/hangup/media | `{ sessionId, fromUserId, type, sdp, candidate, sdpMid, sdpMLineIndex, audio, video }` |
| `clinicalAction` | диагноз / рецепт / справка | `ClinicalActionDto` |

На `videoStarted` вторая сторона: `GET .../video` (или повторный `POST .../video/start`) и поднимает WebRTC. Не использовать ICE/SDP из `videoStarted` — только факт «звонок начат».

---

## 5. WebRTC P2P (`mode: "p2p"`) — обязательный DEV-путь

Два участника. Медиа **peer-to-peer**; бэкенд только сигналит.

### 5.1. Старт

1. `JoinSession(sessionId)`.
2. `getUserMedia({ audio: true, video: true })` — запрос разрешений **на этом же экране**.
3. `POST /video/start` → `iceServers`, `role`.
4. `RTCPeerConnection({ iceServers: iceServers.map(s => ({ urls: s.urls, username: s.username, credential: s.credential })) })`.
5. `addTrack` локальных дорожек; `ontrack` → remote `<video>`.
6. Кто **инициирует** (обычно тот, кто нажал «Начать»): `createOffer` → `setLocalDescription` → `SendRtcSignal(sessionId, { type: "offer", sdp })`.
7. Вторая сторона на `rtcSignal.type === "offer"`: `setRemoteDescription` → `createAnswer` → `SendRtcSignal(..., { type: "answer", sdp })`.
8. ICE: `onicecandidate` → `{ type: "ice", candidate, sdpMid, sdpMLineIndex }`. Входящий `ice` → `addIceCandidate`.

Perfect negotiation: при гонке offer/offer — polite peer (например пациент) откатывает свой offer. Для MVP достаточно: **оффер делает только инициатор** `POST /video/start`, вторая сторона только отвечает.

Mute: `track.enabled = false`; опционально `{ type: "media", audio, video }` собеседнику.

Hangup UI: `SendRtcSignal(..., { type: "hangup" })` **и** `POST /video/stop`. На `videoStopped` / `hangup` — `pc.close()`, остановить `getUserMedia`, **чат не закрывать**.

### 5.2. Чат во время звонка

Тот же `sessionId`. Не открывать второй хаб.  
`POST /messages` + `messageReceived`. Системные (`messageType: "System"`) — диагноз, рецепт, справка, старт/стоп видео.

### 5.3. Ошибки медиа

Нет камеры/микрофона: показать причину, оставить чат. Видео не блокирует переписку.

---

## 6. Режим SFU (`mode: "sfu"`)

Подключаться SDK провайдера по `serverUrl` + `accessToken`.  
Чат и клинические REST — как в p2p.  
`SendRtcSignal` для медиа **не** нужен (сигналинг у SFU). Hub всё равно для чата и `clinicalAction`.

---

## 7. Согласие на видео

Перед первым видео: `POST .../consent` `{ dataProcessingConsent: true, videoRecordingConsent: false }`.  
Запись звонка **не** реализована; `videoRecordingConsent` сохранить в UI, но не обещать запись.

---

## 8. Complete vs действия в звонке

| Действие | Когда | Эффект |
|----------|--------|--------|
| Диагноз / рецепт / справка | сессия открыта, в т.ч. во время видео | сразу в МК / рецепты / чат |
| `POST /complete` | врач заканчивает **приём** | статус `Completed`, протокол, чат и видео недоступны |

В протоколе `/complete` по-прежнему обязательны жалобы, диагноз, рекомендации. Действия из звонка **не заменяют** протокол: врач дублирует итог в complete (или подставляет уже выписанное в форму).

После complete — рейтинг, как в общем ТЗ.

---

## 9. Состояния UI

```
Idle → Starting → InCall → Stopping → Idle
         ↘ Denied (нет getUserMedia) → чат живой
```

| Состояние | Кнопки |
|-----------|--------|
| Idle, `videoActive: false` | «Начать видео» |
| Idle, пришло `videoStarted` | «Присоединиться» (мигает) |
| InCall | mute, стоп видео; у врача — клиника + complete |
| Stopping | disabled, затем Idle |

---

## 10. Проверка (ручная)

```bash
# оба: join + hub JoinSession
curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/join" -H "Authorization: Bearer $TOKEN"

# инициатор
curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/video/start" -H "Authorization: Bearer $TOKEN"

# вторая сторона
curl -s "$GATEWAY/api/v1/consultations/$SESSION/video" -H "Authorization: Bearer $OTHER"

# чат во время звонка
curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/messages" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"messageType":"Text","content":"слышно?"}'

# врач
curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/diagnoses" \
  -H "Authorization: Bearer $DOCTOR" -H 'Content-Type: application/json' \
  -d '{"icd10":"J06.9","text":"ОРВИ"}'
curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/prescriptions" \
  -H "Authorization: Bearer $DOCTOR" -H 'Content-Type: application/json' \
  -d '{"lines":["Парацетамол 500 мг — при температуре"]}'
curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/certificates" \
  -H "Authorization: Bearer $DOCTOR" -H 'Content-Type: application/json' \
  -d '{"type":"HealthStatus","title":"Справка о состоянии","body":"Может посещать занятия."}'

curl -s -X POST "$GATEWAY/api/v1/consultations/$SESSION/video/stop" -H "Authorization: Bearer $TOKEN"
```

Ожидание: два живых потока, сообщения в чате без ухода со звонка, системные строки о диагнозе/рецепте/справке, после stop видео пропало, чат жив.

HTTPS или `localhost` — иначе браузер не отдаст камеру.

---

## 11. Вне этой версии

- Запись звонка, screen share, virtual background.
- Групповой консилиум (больше двух) в p2p — для этого `mode: "sfu"`.
- TURN-сервер (сейчас публичный STUN; за NAT может не хватить — тогда свой TURN в `Sfu:IceServers`).
