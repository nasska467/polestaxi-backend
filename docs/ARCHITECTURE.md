# Архитектура PolesTaxi (C#)

House style — перевод [InnoTaxi ARCHITECTURE.md](https://github.com/GO-Trainee/InnoTaxi/blob/main/ARCHITECTURE.md). Бизнес-логика **не знает** про транспорт, хранилище и внешние сервисы. Зависимости направлены внутрь, к домену.

```
 External World
       │
       ▼
  ┌─────────┐      ┌─────────┐
  │ handler │      │ gateway │
  └────┬────┘      └────┬────┘
       │                │
       ▼                ▼
  ┌──────────────────────────┐
  │         service          │  ← бизнес-логика только здесь
  └──────────┬───────────────┘
             │
             ▼
       ┌──────────┐
       │repository│
       └──────────┘
```

Язык: **C# / .NET**. Слои и имена папок совпадают с шаблоном InnoTaxi (`handler`, `service`, `repository`, `gateway`, `entity`, `app`, `cmd`).

---

## Дерево монорепо

```
polestaxi-backend/
├── PolesTaxi.sln
├── docs/
├── shared/
│   ├── PolesTaxi.Shared/          # Consts, Errors, Types
│   ├── proto/                    # канонические контракты HTTP + gRPC + Kafka
│   ├── graphql/                  # Order Service
│   └── openapi/
└── services/
    └── {service}/                # один csproj = один Go-модуль
        ├── cmd/Program.cs
        ├── config/Config.cs
        ├── app/Run.cs            # composition root
        ├── app/db/{mongo,pg,redis}/
        ├── app/grpc/
        ├── app/kafka/
        ├── entity/{service,repository,gateway}/
        ├── handler/{http,grpc,kafka}/
        ├── gateway/{grpc,http,kafka}/
        ├── service/
        └── repository/{mongo,pg,redis}/
```

**Правила модулей**

- `PolesTaxi.Shared` не ссылается на `services/*`.
- Сервисы ссылаются только на Shared. Друг на друга — **никогда** (только сеть: gRPC / Kafka / HTTP).
- Все конкретные типы создаются в `app/Run.cs`. Остальной код зависит от интерфейсов.

---

## Слои

### `cmd/`

Точка входа. Читает аргументы и вызывает `App.Run`. Без бизнес-логики.

### `config/`

`Config` + загрузка из environment. Передаётся явно, не глобальный singleton.

### `app/`

Composition root: логгер, БД (миграции внутри `New` клиента — если упали, процесс не стартует), gRPC, Kafka, wiring, graceful shutdown (`SIGTERM`/`SIGINT`, drain HTTP/gRPC, flush Kafka).

В этой фазе `Run` — заглушка без подключений.

### `handler/`

Входящие запросы: валидация → `IService` → ответ. Не знает про БД и исходящие вызовы.

Proto — единственный источник правды:

| Транспорт | DTO | Как |
| --- | --- | --- |
| HTTP | protobuf | `google.api.http` + JSON transcoding (аналог gRPC-Gateway) |
| gRPC | protobuf | контракт = `.proto` |
| Kafka | protobuf | `shared/proto/events` |

`handler/http` — регистрация transcoding + эндпоинты, которые нельзя выразить в proto (health, SSE).

### `gateway/` (исходящие)

Вызовы других сервисов и третьих сторон. Методы принимают `CancellationToken` / `context` с timeout.

### `service/`

Бизнес-логика. Зависит только от интерфейсов repository и gateway. Доменные модели — `entity/service` без JSON/DB/proto-атрибутов.

### `repository/`

Доступ к данным. Модели — `entity/repository`. Для Postgres: `Start` / `Finish` / `Abort` (как `PgAtomicRepository` в InnoTaxi).

### `entity/`

Типы по слоям. Нет `entity/http`: HTTP DTO = protobuf.

### `shared/`

- `Consts` — роли, типы такси, имена топиков Kafka
- `Errors` — коды `{ "code", "message" }`
- `Types` — enum домена + `ToString` / `FromString`; `ToPb`/`FromPb` появятся после генерации proto (фаза 3–5)

---

## Поток HTTP (через JSON transcoding)

```
HTTP JSON
  → transcoding (proto request)
  → handler/grpc
  → map → entity/service
  → service (правила)
  → repository и/или gateway
  → map → proto response
  → JSON клиенту
```

Kafka: bytes protobuf → `handler/kafka` → тот же `service`.

Исходящее событие: `service` → `gateway/kafka` → топик.

---

## Сервисы и хранилища

| Сервис | БД | HTTP | Синхрон | Асинхрон |
| --- | --- | --- | --- | --- |
| User | MongoDB | REST | gRPC (Auth, Driver, Wallet) | `user.registered` |
| Auth | Redis | REST | gRPC Validate; User.VerifyCredentials | — |
| Driver | MongoDB | REST | gRPC от User и Order | `driver.registered` |
| Order | Elasticsearch + Redis cache | REST + GraphQL | gRPC Driver, Wallet | order / payment events |
| Wallet | PostgreSQL + Redis cache | REST (часть internal) | gRPC от User/Order | `payment.completed` |
| Analytic | ClickHouse | REST (роль Analyst) | — | Kafka consumer |
| Gateway | нет | NGINX + YARP | gRPC Auth.Validate, прокси | — |

Роли: `User`, `Driver`, `Analyst`, `Admin` (несколько ролей у одного пользователя). Типы такси: `Economy`, `Comfort`, `Business`.

Проверка JWT на Gateway через Auth. Downstream-сервисы в спеке не дублируют auth; Gateway режет по ролям.

---

## Наблюдаемость и устойчивость (контракт на будущее)

Prometheus, структурированные логи, Jaeger. Circuit breaker / retry / timeout — Polly. Health: liveness + readiness. Это внедряется в фазах реализации, не здесь.

---

## Что не делать в слоях

- `service` не импортирует `handler` и конкретные клиенты БД
- Маппинг домен ↔ БД живёт в `repository`, не `ToRepository()` на домене
- Доменные ошибки сервиса — в сервисе; в Shared только общие коды
- Timeout на каждый исходящий вызов gateway
