# Схемы данных PolesTaxi

Карта сервисов, хранилищ и связей: [databases.drawio](databases.drawio).

## Общие правила

Каждый сервис **владеет** только своей базой. Другой сервис не читает чужие таблицы и коллекции напрямую.

- **JOIN между сервисами запрещён.** Если Order знает пассажира, он хранит копию идентификатора `userId`, а не внешний ключ в Mongo User.
- **Один идентификатор — строка.** `userId` в Driver, Order и Wallet — это `User._id`, записанный текстом. Физического FK между Mongo, Redis, Elasticsearch и PostgreSQL нет.
- **Разный движок — разная задача.** Документ профиля, ключ сессии, геопоиск заказа, денежные ограничения и аналитика по событиям не живут в одной Postgres «на всё».

Строковые значения ролей и типов такси совпадают с `PolesTaxi.Shared`: `User`, `Driver`, `Analyst`, `Admin`; `Economy`, `Comfort`, `Business`.

На схеме зелёные стрелки — запись в своё хранилище, красные — RPC между сервисами, оранжевые — публикация в Kafka, фиолетовые — подписка Analytic на Kafka.

---

## Сводка

| Сервис   | Своё хранилище                       | Что хранит                   | С кем связан                                  |
| -------- | ------------------------------------ | ---------------------------- | --------------------------------------------- |
| Gateway  | нет                                  | не хранит данные             | RPC ко всем сервисам (вход с края)            |
| Auth     | Redis                                | access/refresh-токены        | RPC к User (проверка логина)                  |
| User     | MongoDB `polestaxi_users`            | профили, пароль, роли        | публикует события в Kafka; RPC с Wallet       |
| Driver   | MongoDB `polestaxi_drivers`          | анкета водителя, статус, geo | публикует в Kafka; RPC с Order                |
| Order    | Elasticsearch `orders` (+ кэш Redis) | заказы и поездки             | RPC к User, Driver, Wallet; публикует в Kafka |
| Wallet   | PostgreSQL                           | кошелёк и проводки           | RPC с User и Order                            |
| Analytic | ClickHouse                           | факты для отчётов            | подписывается на Kafka                        |

Prometheus собирает метрики **со всех** сервисов, Grafana их показывает. Это наблюдаемость, не бизнес-база: своей доменной модели у них нет.

---

## Gateway Service

**Зачем.** Единая точка входа: клиент не ходит в User или Order напрямую.

**Хранилище.** Нет. На схеме это «From all services / To all services»: Gateway проксирует RPC, сам ничего не сохраняет.

---

## Auth Service

**Зачем.** Выдать пару JWT после проверки логина и пароля, помнить живые сессии, уметь отозвать их при выходе.

**Хранилище.** Redis — ключ–значение с TTL. Сессию не кладут в Mongo: проверка «токен ещё жив» должна быть быстрой, а просрочка — автоматической.

**Связи.** RPC к User Service: «этот login и пароль верны?». Своих профилей Auth не хранит.

| Ключ            | TTL       | Значение                           |
| --------------- | --------- | ---------------------------------- |
| `access:{jti}`  | 15–30 мин | JSON `{ userId, roles[] }`         |
| `refresh:{jti}` | 7–30 дней | JSON `{ userId, roles[], family }` |

`jti` — уникальный идентификатор токена. Logout удаляет **оба** ключа. Refresh выписывает новую пару и стирает старые `jti` (ротация): украденным refresh нельзя пользоваться бесконечно.

---

## User Service

**Зачем.** Учётная запись человека: кто он, как войти, какие роли. Имя, телефон, почта и пароль живут **только здесь**.

**Хранилище.** MongoDB, база `polestaxi_users`, коллекция `users`. Документ удобен для профиля с массивом ролей и историей оценок.

**Связи.** Публикует в Kafka событие регистрации. По RPC связан с Wallet: у пользователя должен появиться кошелёк с тем же `userId`.

### Коллекция `users`

| Поле            | Тип      | Обязательное | Ограничения                                 |
| --------------- | -------- | ------------ | ------------------------------------------- |
| `_id`           | ObjectId | да           | первичный ключ                              |
| `name`          | string   | да           | длина 1–100                                 |
| `phone`         | string   | да           | уникальный                                  |
| `email`         | string   | да           | уникальный, формат email                    |
| `passwordHash`  | string   | да           | bcrypt или argon2, не открытый пароль       |
| `roles`         | string[] | да           | только `User`, `Driver`, `Analyst`, `Admin` |
| `rating`        | double   | да           | 0–5, среднее по последним 20 поездкам       |
| `recentRatings` | double[] | да           | не больше 20 элементов, FIFO                |
| `isDeleted`     | bool     | да           | soft-delete, по умолчанию `false`           |
| `createdAt`     | datetime | да           |                                             |
| `updatedAt`     | datetime | да           |                                             |

Индексы: уникальные `email` и `phone`; индекс `{ isDeleted: 1 }` для выборки живых учёток.

**Инварианты.** Публичная регистрация ставит только `User` или `Driver` (`RegistrationAllowed` в Shared). Analyst и Admin не создаются формой. После «удаления» документ остаётся, `isDeleted = true`.

---

## Driver Service

**Зачем.** То, чего нет в профиле пассажира: тип такси, номер, смена статуса, координаты. Один человек — не больше одной анкеты водителя.

**Хранилище.** Отдельная MongoDB `polestaxi_drivers`, коллекция `drivers`. Это **не** та же база, что у User: так проще масштабировать geo-поиск и не смешивать пароли с координатами.

**Связи.** `userId` указывает на User.\_id (логическая связь, пунктир на схеме). Order ходит сюда по RPC, чтобы найти свободного водителя и провести поездку. События регистрации водителя уходят в Kafka.

### Коллекция `drivers`

| Поле           | Тип           | Обязательное | Ограничения                                |
| -------------- | ------------- | ------------ | ------------------------------------------ |
| `_id`          | ObjectId      | да           | первичный ключ                             |
| `userId`       | string        | да           | уникальный, логическая ссылка на User.\_id |
| `taxiType`     | string        | да           | `Economy`, `Comfort` или `Business`        |
| `licensePlate` | string        | да           | госномер                                   |
| `status`       | string        | да           | `offline`, `available` или `on-trip`       |
| `location`     | GeoJSON Point | нет          | индекс `2dsphere`                          |
| `createdAt`    | datetime      | да           |                                            |
| `updatedAt`    | datetime      | да           |                                            |

Имя, email, телефон и пароль **сюда не копируют** — они в User.

Индексы: уникальный `userId`; составной `{ status: 1, taxiType: 1 }` (поиск свободных нужного класса); `2dsphere` на `location`.

**Жизненный цикл статуса** (как `EnumMaps.CanTransition` в Shared):

```
offline  ↔  available  →  on-trip  →  available | offline
```

Запрещено: `offline` → `on-trip`. Нельзя взять заказ, не выйдя на линию. Такой переход — конфликт (HTTP 409).

---

## Order Service

**Зачем.** Жизненный цикл поездки: от создания заказа до оценки. Нужен поиск по гео и статусу, поэтому не Mongo, а Elasticsearch.

**Хранилище.** Индекс `orders`. Рядом кэш Redis `order:{id}` с TTL — чтобы часто открываемый заказ не каждый раз искать в Elasticsearch.

**Связи.** RPC к User, Driver и Wallet (на схеме красные стрелки от Order). Публикует смену статуса в Kafka — Analytic потом посчитает завершённые поездки.

### Документ заказа

| Поле                                                 | Тип                  | Обязательное | Ограничения                      |
| ---------------------------------------------------- | -------------------- | ------------ | -------------------------------- |
| `id`                                                 | keyword              | да           | первичный ключ                   |
| `userId`                                             | keyword              | да           | пассажир, копия User.\_id        |
| `driverId`                                           | keyword              | нет          | пусто до назначения водителя     |
| `taxiType`                                           | keyword              | да           | `Economy`, `Comfort`, `Business` |
| `status`                                             | keyword              | да           | см. жизненный цикл               |
| `pickup` / `dropoff`                                 | geo_point + адрес    | да           | точки посадки и высадки          |
| `route`                                              | geo_shape / polyline | нет          |                                  |
| `distanceMeters`                                     | long                 | да           | ≥ 0                              |
| `durationSeconds`                                    | long                 | да           | ≥ 0                              |
| `price`                                              | scaled_float         | да           | ≥ 0                              |
| `surge`                                              | float                | да           | ≥ 1.0                            |
| `userRating` / `driverRating`                        | byte                 | нет          | 1–5, только после `completed`    |
| `comment`                                            | text                 | нет          |                                  |
| `createdAt`, `assignedAt`, `startedAt`, `finishedAt` | date                 | частично     | метки этапов                     |

**Жизненный цикл** (как `OrderStatus` в Shared):

```
created → driver_assigned → in_progress → completed
                                      ↘ cancelled
```

Кэш: ключ `order:{id}` в Redis, TTL на недавние заказы.

---

## Wallet Service

**Зачем.** Деньги нельзя «примерно». Баланс не кладут в документ User: финансовый сервис отдельно, с проверками на уровне SQL.

**Хранилище.** PostgreSQL, схема `wallet`. Таблицы связаны **внутри** этого сервиса обычным FK. На User указывает только текстовый `user_id`.

**Связи.** RPC с User (создать кошелёк при регистрации) и с Order (списание / возврат за поездку).

### Таблица `wallets`

| Поле         | Тип            | Обязательное | Ограничения                           |
| ------------ | -------------- | ------------ | ------------------------------------- |
| `id`         | UUID           | да           | первичный ключ                        |
| `user_id`    | TEXT           | да           | уникальный, логическая ссылка на User |
| `balance`    | NUMERIC(12, 2) | да           | `>= 0`                                |
| `currency`   | CHAR(3)        | да           | по умолчанию `RUB`                    |
| `created_at` | TIMESTAMPTZ    | да           | по умолчанию `now()`                  |

### Таблица `transactions`

| Поле         | Тип            | Обязательное | Ограничения                                     |
| ------------ | -------------- | ------------ | ----------------------------------------------- |
| `id`         | UUID           | да           | первичный ключ                                  |
| `wallet_id`  | UUID           | да           | FK на `wallets.id`                              |
| `type`       | TEXT           | да           | `debit`, `credit` или `refund`                  |
| `amount`     | NUMERIC(12, 2) | да           | `> 0`                                           |
| `status`     | TEXT           | да           | `pending`, `completed`, `failed`, `rolled_back` |
| `order_id`   | TEXT           | нет          | логическая ссылка на заказ                      |
| `created_at` | TIMESTAMPTZ    | да           | по умолчанию `now()`                            |

Индекс: `(wallet_id, created_at DESC)` — лента операций кошелька.

DDL:

```sql
CREATE TABLE wallets (
    id          UUID PRIMARY KEY,
    user_id     TEXT NOT NULL UNIQUE,
    balance     NUMERIC(12, 2) NOT NULL CHECK (balance >= 0),
    currency    CHAR(3) NOT NULL DEFAULT 'RUB',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE transactions (
    id          UUID PRIMARY KEY,
    wallet_id   UUID NOT NULL REFERENCES wallets(id),
    type        TEXT NOT NULL CHECK (type IN ('debit', 'credit', 'refund')),
    amount      NUMERIC(12, 2) NOT NULL CHECK (amount > 0),
    status      TEXT NOT NULL CHECK (status IN ('pending', 'completed', 'failed', 'rolled_back')),
    order_id    TEXT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_transactions_wallet_created ON transactions (wallet_id, created_at DESC);
```

Каждая проводка пишется в `transactions`: так остаётся аудит, а не только текущее число в `balance`.

---

## Analytic Service

**Зачем.** Отчёты и дашборд. Это не OLTP: сервис не принимает «создать заказ», он **читает поток событий**.

**Хранилище.** ClickHouse, движок MergeTree. Колоночное хранение хорошо считает агрегаты по большому числу фактов.

**Связи.** На схеме фиолетовая стрелка Kafka → Analytic: только подписка. В чужие Mongo и Postgres Analytic не ходит.

### Таблицы фактов

| Таблица           | Поля                                                                               | Откуда событие                                          |
| ----------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------- |
| `user_registered` | `user_id`, `role`, `event_time`                                                    | топик `user.registered`                                 |
| `order_completed` | `order_id`, `user_id`, `driver_id`, `taxi_type`, `price`, `distance`, `event_time` | топик `order.status_changed` (когда статус `completed`) |
| `trip_rated`      | `order_id`, `from_role`, `score`, `event_time`                                     | топик `trip.rated`                                      |

Имена топиков — как в `PolesTaxi.Shared.Consts.KafkaTopics`.
