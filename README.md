# Event Manager — backend

## Описание

Event Manager — система управления мероприятиями и бронированиями на ASP.NET Core. Система разделена на три независимых сервиса, каждый из которых использует собственную базу PostgreSQL.

| Сервис | Назначение | HTTP | HTTPS | База данных | Порт БД |
|---|---|---:|---:|---|---:|
| Users (`Auth.Presentation`) | Регистрация, вход и выдача JWT | `5283` | `7066` | `event_manager_users` | `5433` |
| `Event.Presentation` | Управление мероприятиями и свободными местами | `5018` | `7183` | `event_manager_events` | `5434` |
| `Booking.Presentation` | Создание, подтверждение и отмена бронирований | `5223` | `7111` | `event_manager_bookings` | `5435` |

Сервисы взаимодействуют асинхронно через Kafka, доступную локально на порту `9092`.

### Сервис Users (Auth)

Сервис Users представлен проектами `Auth.Domain`, `Auth.Application`, `Auth.Infrastructure` и `Auth.Presentation`. Он отвечает только за пользователей и аутентификацию.

Основные обязанности:

- сохраняет пользователей в собственной базе `event_manager_users`;
- проверяет уникальность логина при регистрации;
- хеширует пароль перед сохранением и не хранит его в открытом виде;
- проверяет логин и пароль при авторизации;
- создаёт JWT с идентификатором пользователя, логином, ролью и уникальным идентификатором токена;
- является единственным сервисом, который выдаёт JWT.

Auth не подключён к Kafka и не обращается к базам Events или Bookings. После выдачи токена сервисы Events и Bookings самостоятельно проверяют JWT по общим `Secret`, `Issuer` и `Audience`.

### Сервис Events

Сервис Events представлен проектами `Event.Domain`, `Event.Application`, `Event.Infrastructure` и `Event.Presentation`. Он является владельцем данных мероприятий и единственным сервисом, который изменяет количество свободных мест.

Основные обязанности:

- хранит мероприятия в базе `event_manager_events`;
- возвращает список мероприятий с фильтрацией и пагинацией;
- позволяет администраторам создавать, изменять и удалять мероприятия;
- проверяет JWT, выданный сервисом Auth;
- принимает через Kafka сообщения о подтверждении и отмене бронирований;
- атомарно уменьшает или увеличивает `AvailableSeats`;
- отправляет `BookingRejected`, если подтвердить место невозможно.

Events не изменяет записи в базе Bookings и не вызывает Booking API напрямую. Результат обработки передаётся обратно только через Kafka.

В сервисе работают два hosted-сервиса:

- `KafkaTopicInitializerService` создаёт Kafka-топики при старте приложения;
- `BookingEventsConsumerWorker` постоянно читает сообщения `BookingConfirmed` и `BookingCancelled`.

### Сервис Bookings

Сервис Bookings представлен проектами `Booking.Domain`, `Booking.Application`, `Booking.Infrastructure` и `Booking.Presentation`. Он является владельцем полного жизненного цикла бронирования.

Основные обязанности:

- хранит бронирования в базе `event_manager_bookings`;
- создаёт новую бронь со статусом `Pending`;
- получает идентификатор пользователя из JWT claims;
- ограничивает количество активных броней пользователя;
- подтверждает ожидающие брони в фоновом процессе;
- после сохранения подтверждения публикует `BookingConfirmed`;
- публикует `BookingCancelled` при отмене подтверждённой брони;
- принимает `BookingRejected` от Events и переводит бронь в `Rejected`.

Bookings не проверяет наличие мероприятия и свободных мест прямым HTTP-запросом. Сервис сначала сохраняет собственное состояние, а согласование количества мест выполняется асинхронно через Kafka.

В сервисе работают два фоновых процесса:

- `BookingConfirmationBackgroundService` каждые 15 секунд обрабатывает брони `Pending`;
- `BookingEventsConsumerWorker` постоянно читает ответы `BookingRejected` от Events.

**Основные функции:**

- регистрация пользователей и выдача JWT в сервисе Auth;
- просмотр мероприятий и управление ими с разграничением прав;
- создание и автоматическое подтверждение бронирований;
- синхронизация статусов брони и количества свободных мест через Kafka;
- освобождение места при отмене подтверждённой брони;
- глобальная обработка ошибок через middleware.

## Требования

- **Docker и Docker Compose**: для запуска всего стека одной командой;
- **.NET SDK 10**: только для локального запуска сервисов без API-контейнеров;
- свободные порты `5283`, `5018`, `5223`, `5433`–`5435` и `9092`.

## Быстрый старт

### 1. Запустить весь стек

Из корня репозитория выполните:

```bash
docker compose up -d --build
```

Команда собирает и запускает:

- сервис Auth;
- сервис Events;
- сервис Bookings;
- три отдельные базы PostgreSQL;
- Kafka и ZooKeeper.

Для локальной разработки Compose использует общий JWT-секрет по умолчанию. Его можно переопределить через переменную окружения `JWT_SECRET` или файл `.env`:

```text
JWT_SECRET=your-shared-secret-at-least-32-bytes-long
```

### 2. Проверить состояние


```bash
docker compose ps
```

Посмотреть логи всех сервисов:

```bash
docker compose logs -f
```

Посмотреть логи отдельного сервиса:

```bash
docker compose logs -f auth
docker compose logs -f events
docker compose logs -f bookings
```

Swagger UI:

- Auth: `http://localhost:5283/swagger`;
- Events: `http://localhost:5018/swagger`;
- Bookings: `http://localhost:5223/swagger`.

Миграции каждой базы применяются автоматически при старте соответствующего API-контейнера. Events также создаёт Kafka-топики `booking-events` и `booking-rejected`.

### 3. Остановить стек

Остановить контейнеры без удаления данных:

```bash
docker compose down
```

Остановить контейнеры и удалить тома PostgreSQL:

```bash
docker compose down -v
```

### 4. Локальный запуск без API-контейнеров

Если сервисы нужно запускать через `dotnet run` или IDE, поднимите только инфраструктуру:

```bash
docker compose up -d zookeeper kafka users-db events-db bookings-db
```

Задайте локальные строки подключения через User Secrets:


После этого запустите сервисы в трёх терминалах:

```bash
dotnet run --project Auth.Presentation --launch-profile http
dotnet run --project Event.Presentation --launch-profile http
dotnet run --project Booking.Presentation --launch-profile http
```

При локальном запуске приложения подключаются к Kafka через `localhost:9092`. Внутри Docker используется адрес `kafka:29092`.

## Конфигурация

Каждый сервис читает собственную строку подключения из `ConnectionStrings:DefaultConnection`. В Docker Compose строки подключения передаются через `ConnectionStrings__DefaultConnection` и используют внутренние имена `users-db`, `events-db` и `bookings-db`.

При локальном запуске используются `localhost` и внешние порты `5433`, `5434` и `5435`.

### JWT

Токен выдаёт только сервис Auth. Сервисы Events и Bookings проверяют его подпись, издателя, аудиторию и срок действия.

Идентификатор пользователя хранится в claim `sub`, а роль — в claim `role`. Автоматическое преобразование claim-типов отключено, поэтому генератор токена, JWT-валидаторы и контроллер Bookings используют одинаковые имена.

Во всех трёх сервисах секция `TokenSettings` должна содержать одинаковые значения `Secret`, `Issuer` и `Audience`:

```json
{
  "TokenSettings": {
    "Secret": "shared-secret-at-least-32-bytes-long",
    "Issuer": "EventManagerApi",
    "Audience": "EventManagerApi",
    "LifeTimeInMinutes": 15
  }
}
```

Для production передавайте секрет через `TokenSettings__Secret` или внешнее хранилище секретов.

### Кеширование Events

Redis используется сервисом Events как необязательный кеш: PostgreSQL остаётся источником истины. Параметры подключения находятся в секции `Redis`, а время жизни и ключи — в `EventCache`:

```json
{
  "Redis": {
    "Host": "localhost:6379",
    "ConnectTimeout": 5000,
    "SyncTimeout": 3000,
    "AbortOnConnectFail": false
  },
  "EventCache": {
    "EventByIdTtl": "00:02:00",
    "TopEventsTtl": "00:10:00",
    "EventKeyPrefix": "event",
    "TopEventsKey": "events:top10"
  }
}
```

**Стратегия:**

- `GET /events/{id}` и получение топ-событий используют read-through кеш: при попадании ответ строится из Redis, при промахе данные читаются из репозитория и сохраняются с TTL;
- ключ отдельного события имеет вид `event:{id}`; при обновлении он перезаписывается актуальным значением, при удалении — инвалидируется;
- общий ключ `events:top10` инвалидируется после создания, обновления или удаления события и наполняется заново при следующем запросе;
- список топ-событий инвалидируется, а не изменяется точечно, поскольку любая мутация может изменить состав или порядок списка. Это исключает сложную и подверженную ошибкам синхронизацию нескольких кешированных записей.

Redis не является обязательной зависимостью для запуска. Подключение создаётся лениво при первом обращении к кешу; если оно не может быть создано, сервис переключается на no-op кеш. Ошибки операций Redis также обрабатываются как промах кеша: запросы продолжают работать через репозиторий, а изменения данных сохраняются в PostgreSQL.

### Kafka

Сервисы Events и Bookings используют секцию `Kafka`:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroup": "events-service",
    "EnableAutoCommit": false
  }
}
```

- Events использует группу потребителей `events-service`;
- Bookings использует группу потребителей `bookings-service`;
- топик `booking-events` содержит сообщения `BookingConfirmed` и `BookingCancelled`;
- топик `booking-rejected` содержит сообщения `BookingRejected`;
- Events создаёт оба топика при старте, если они ещё не существуют.

**Компоненты Kafka:**

| Компонент | Сервис | Тип | Назначение |
|---|---|---|---|
| `KafkaTopicInitializerService` | Events | `IHostedService` | Создаёт `booking-events` и `booking-rejected` до запуска подписчика |
| `BookingProducer` | Bookings | Singleton, `IDisposable` | Публикует `BookingConfirmed` и `BookingCancelled` |
| `BookingEventsConsumerWorker` | Events | `BackgroundService` | Читает `booking-events` и изменяет свободные места |
| `BookingProducer` | Events | Singleton, `IDisposable` | Публикует `BookingRejected` |
| `BookingEventsConsumerWorker` | Bookings | `BackgroundService` | Читает `booking-rejected` и отклоняет бронь |

**Как запускаются подписчики:**

1. ASP.NET Core создаёт hosted-сервисы вместе с приложением.
2. Events сначала запускает `KafkaTopicInitializerService` и пытается создать оба топика через Kafka Admin Client.
3. Ошибка создания топика записывается в лог, но не останавливает сервис. Существующий топик считается нормальным случаем.
4. После этого `BookingEventsConsumerWorker` подписывается на свой топик.
5. Блокирующий вызов `consumer.Consume(...)` выполняется внутри отдельной задачи, поэтому не блокирует запуск HTTP-приложения.
6. При остановке приложения cancellation token завершает цикл чтения, consumer закрывается штатно.

**Обработка зависимостей и offsets:**

- `BackgroundService` создаётся как singleton;
- `DbContext`, репозитории и обработчики сообщений зарегистрированы как scoped;
- поэтому consumer создаёт новый DI scope для каждого сообщения и получает репозиторий из этого scope;
- автоматический commit отключён;
- offset фиксируется вручную после завершения обработки сообщения;
- если обработчик выбросил исключение, ошибка логируется, а consumer продолжает работу.

**Формат публикации:**

- событие сериализуется в JSON;
- ключ сообщения равен `EventId`;
- `BookingConfirmed` и `BookingCancelled` различаются заголовком `message-type` внутри общего топика `booking-events`;
- продюсеры используют `Acks.All` и идемпотентный режим Kafka;
- продюсер создаётся один раз на сервис, при остановке вызывает `Flush` и освобождает соединение.

### Роли и доступ

- `GET /events` и `GET /events/{id}` доступны без JWT;
- `POST /events`, `PUT /events/{id}` и `DELETE /events/{id}` доступны только роли `Admin`;
- все эндпоинты Bookings требуют JWT;
- при создании брони идентификатор пользователя читается из claims токена;
- пользователь может отменить только собственную бронь, а `Admin` — бронь любого пользователя.

### Получение JWT через Swagger

1. Откройте Swagger сервиса Auth: `http://localhost:5283/swagger`.
2. Вызовите `POST /auth/register`.
3. Вызовите `POST /auth/login` и скопируйте `token` из ответа.
4. В Swagger сервиса Events или Bookings нажмите `Authorize`.
5. Вставьте JWT без префикса `Bearer`.

## API Endpoints

### Аутентификация

**Сервис:** Auth. **Базовый путь:** `/auth`.

- `POST /auth/register` — зарегистрировать пользователя;
- `POST /auth/login` — получить JWT.

### Мероприятия

**Сервис:** Events. **Базовый путь:** `/events`.

- `GET /events` — получить мероприятия с фильтрацией по `title`, `from`, `to` и пагинацией;
- `GET /events/{id}` — получить мероприятие по идентификатору;
- `POST /events` — создать мероприятие, требуется роль `Admin`;
- `PUT /events/{id}` — изменить мероприятие, требуется роль `Admin`;
- `DELETE /events/{id}` — удалить мероприятие, требуется роль `Admin`.

### Бронирования

**Сервис:** Bookings. **Базовый путь:** `/bookings`. Все запросы требуют JWT.

- `POST /bookings/{eventId}/book` — создать бронь со статусом `Pending`, ответ `202 Accepted`;
- `GET /bookings/{id}` — получить бронь по идентификатору;
- `DELETE /bookings/{id}` — отменить бронь.

## Модели данных

**Мероприятие:**

- `Id` — идентификатор;
- `Title` и `Description` — название и описание;
- `StartAt` и `EndAt` — время проведения;
- `TotalSeats` — общее количество мест;
- `AvailableSeats` — количество свободных мест.

**Бронирование:**

- `Id` — идентификатор брони;
- `EventId` — идентификатор мероприятия;
- `UserId` — идентификатор пользователя;
- `Status` — текущий статус;
- `CreatedAt` и `ProcessedAt` — время создания и обработки.

| Статус | Описание |
|---|---|
| `Pending` | Бронь создана и ожидает обработки |
| `Confirmed` | Бронь подтверждена сервисом Bookings |
| `Rejected` | Бронь отклонена Bookings или после ответа Events |
| `Cancelled` | Бронь отменена пользователем или администратором |

## Архитектура (Clean Architecture)

Каждый из сервисов Auth, Events и Bookings разделён на четыре слоя. Общие Kafka-контракты находятся в `EventManager.Contracts`, а общие DTO и перечисления — в `EventManager.Common`.

### 1. Domain

Проекты `Auth.Domain`, `Event.Domain` и `Booking.Domain` содержат сущности, перечисления, бизнес-правила и доменные исключения. Domain не зависит от Infrastructure или Presentation.

### 2. Application

Проекты `Auth.Application`, `Event.Application` и `Booking.Application` содержат сценарии использования, DTO и интерфейсы репозиториев и Kafka-издателей.

### 3. Infrastructure

Проекты `Auth.Infrastructure`, `Event.Infrastructure` и `Booking.Infrastructure` содержат EF Core, PostgreSQL-репозитории, миграции и реализации интеграции с Kafka.

Kafka-подписчики Events и Bookings реализованы как `BackgroundService`. Для получения scoped-репозиториев каждый обработанный Kafka message создаёт отдельный DI scope.

Ответственность Infrastructure по сервисам:

- Auth: `AppDbContext`, репозиторий пользователей, хеширование паролей и генерация JWT;
- Events: `AppDbContext`, репозиторий мероприятий, атомарное изменение мест, Kafka consumer, producer и создание топиков;
- Bookings: `AppDbContext`, репозиторий броней, Kafka producer и consumer ответов Events.

### 4. Presentation

Проекты `Auth.Presentation`, `Event.Presentation` и `Booking.Presentation` содержат контроллеры, middleware, настройку JWT, Swagger и точки входа приложений.

В `Booking.Presentation` также работает фоновый сервис, который периодически обрабатывает брони со статусом `Pending`.

## Бизнес-логика

### Сценарий использования

1. Пользователь регистрируется и получает JWT в Auth.
2. Пользователь выбирает мероприятие через Events.
3. `POST /bookings/{eventId}/book` создаёт запись `Pending` в базе Bookings.
4. Перед созданием Bookings проверяет количество активных броней пользователя. Максимальное количество — 10.
5. Фоновый обработчик Bookings раз в 15 секунд получает из базы все брони `Pending`.
6. Для каждой брони обработчик повторно проверяет ограничение и переводит допустимую бронь в `Confirmed`.
7. Статус сохраняется в базе Bookings до публикации сообщения.
8. Сервисы согласуют бронь и свободные места сообщениями Kafka.
9. Итоговый статус можно получить через `GET /bookings/{bookingId}`.

### Поток данных BookingConfirmed

1. Фоновый обработчик Bookings переводит бронь из `Pending` в `Confirmed` и сначала сохраняет изменение в базе `event_manager_bookings`.
2. После успешного сохранения Bookings формирует `BookingConfirmed` с `BookingId`, `EventId`, `UserId`, количеством мест и временем обработки.
3. Сообщение сериализуется в JSON и публикуется в топик `booking-events` с заголовком `message-type=booking-confirmed`.
4. Ключ сообщения равен `EventId`, поэтому все подтверждения и отмены для одного мероприятия попадают в один partition и обрабатываются по порядку.
5. Events подписан на `booking-events` в группе `events-service` и блокирующим вызовом Kafka ожидает следующее сообщение.
6. После получения Events читает заголовок `message-type`, десериализует `BookingConfirmed` и создаёт scoped-обработчик.
7. Обработчик проверяет обязательные идентификаторы и положительное количество мест.
8. Events загружает мероприятие и пытается одним SQL-обновлением уменьшить `AvailableSeats`, только если свободных мест достаточно.
9. При успешном обновлении Events записывает результат в лог, после чего consumer фиксирует offset.

```text
BookingConfirmationBackgroundService
        │
        ├─ сохраняет Confirmed в bookings-db
        │
        └─ BookingConfirmed → booking-events → Events consumer
                                                │
                                                └─ уменьшает AvailableSeats в events-db
```

### Отклонение брони через BookingRejected

Если Events не может применить `BookingConfirmed`, используется обратное событие:

1. Events обнаруживает, что мероприятие отсутствует, мест недостаточно или атомарное обновление не выполнилось.
2. Events не обращается к базе Bookings напрямую.
3. Events формирует `BookingRejected` и публикует его в топик `booking-rejected`.
4. Bookings consumer получает сообщение в группе `bookings-service`.
5. Consumer создаёт scope, загружает бронь по `BookingId` и проверяет её текущий статус.
6. Если бронь ещё не отменена и не отклонена, Bookings переводит её в `Rejected` и сохраняет изменение в собственной базе.
7. Если бронь не найдена или уже завершена, сообщение пропускается с записью в лог.

```text
Events consumer
      │
      └─ BookingRejected → booking-rejected → Bookings consumer
                                                 │
                                                 └─ сохраняет Rejected в bookings-db
```

### Отмена брони и освобождение места

1. Пользователь или администратор вызывает `DELETE /bookings/{id}`.
2. Bookings проверяет существование брони, права пользователя и возможность перехода в `Cancelled`.
3. Bookings сначала сохраняет статус `Cancelled` в собственной базе.
4. Если до отмены бронь имела статус `Confirmed`, сервис публикует `BookingCancelled` в `booking-events`.
5. Events consumer определяет тип сообщения по заголовку `message-type=booking-cancelled`.
6. Events атомарно увеличивает `AvailableSeats`, не позволяя значению превысить `TotalSeats`.
7. Если отменяется `Pending`-бронь, сообщение не публикуется, потому что место в Events ещё не занималось.

### Синхронизация и защита от гонок

- уменьшение мест выполняется SQL-условием `AvailableSeats >= AmountSeats`;
- увеличение мест выполняется SQL-условием `AvailableSeats + AmountSeats <= TotalSeats`;
- проверка и изменение выполняются одной командой `ExecuteUpdateAsync`, поэтому параллельные consumer-операции не могут зарезервировать одно место дважды;
- Kafka-сообщения используют `EventId` как ключ и сохраняют порядок внутри partition;
- Events и Bookings используют разные consumer groups, поскольку обрабатывают разные типы сообщений;
- consumer offsets фиксируются вручную после обработки;
- Kafka-продюсеры зарегистрированы как singleton и освобождаются при остановке приложения;
- исключение одного сообщения логируется и не завершает весь фоновый worker.

### Обработка конфликтов

Система использует eventual consistency: сразу после фонового подтверждения бронь может короткое время иметь статус `Confirmed`, пока Events ещё не обработал сообщение. Если место зарезервировать невозможно, обратное сообщение `BookingRejected` переводит её в итоговый статус `Rejected`.

Если Events не может зарезервировать место после получения `BookingConfirmed`, сервис не падает и не изменяет базу Bookings напрямую. Он отправляет `BookingRejected`, после чего Bookings обновляет собственную запись.

Если подтверждённая бронь отменена, место освобождается асинхронно после обработки `BookingCancelled` сервисом Events.

## Тестирование

Проект содержит отдельные модульные и интеграционные тесты для каждого сервиса.

### Модульные тесты

```bash
dotnet test Auth.UnitTests/Auth.UnitTests.csproj
dotnet test Event.UnitTests/Event.UnitTests.csproj
dotnet test Booking.UnitTests/Booking.UnitTests.csproj
```

### Интеграционные тесты

Для интеграционных тестов требуется запущенный Docker, так как используется Testcontainers.

```bash
dotnet test Auth.IntegrationTests/Auth.IntegrationTests.csproj
dotnet test Event.IntegrationTests/Event.IntegrationTests.csproj
dotnet test Booking.IntegrationTests/Booking.IntegrationTests.csproj
```

### Запуск всех тестов

```bash
dotnet test EventManager.sln
```

## Миграции базы данных

Миграции находятся в каталогах:

- `Auth.Infrastructure/Migrations`;
- `Event.Infrastructure/Migrations`;
- `Booking.Infrastructure/Migrations`.

При старте каждый сервис автоматически применяет миграции к собственной базе.

### Создание новой миграции

Пример для сервиса Events:

```bash
dotnet ef migrations add <MigrationName> \
  --project Event.Infrastructure \
  --startup-project Event.Presentation
```

Для Auth или Bookings замените оба имени проектов на соответствующие.

### Применение миграций

Пример для сервиса Events:

```bash
dotnet ef database update \
  --project Event.Infrastructure \
  --startup-project Event.Presentation
```

## Запуск в IDE

- откройте `EventManager.sln` в Visual Studio или Rider;
- настройте одновременный запуск `Auth.Presentation`, `Event.Presentation` и `Booking.Presentation`;
- сначала запустите зависимости командой `docker compose up -d`;
- запустите все три Presentation-проекта;
- используйте отдельный Swagger UI каждого сервиса.
