# Event Manager — backend

## Описание

ASP.NET Core Web API для управления мероприятиями и бронированиями, построенный на принципах чистой архитектуры (Clean Architecture).

**Основные функции:**

- CRUD операции для мероприятий с фильтрацией и пагинацией
- Система бронирования с автоматическим подтверждением через фоновый сервис
- Глобальная обработка ошибок через middleware
- Синхронизация и защита от гонок при одновременных бронированиях

## Требования

- **.NET SDK**: .NET 10
- **PostgreSQL**: локально или в Docker (см. `docker-compose.yml`)

## Быстрый старт

### 1. Запустить PostgreSQL

```bash
docker compose up -d
```

### 2. Запустить приложение

```bash
dotnet restore
dotnet build
dotnet run --project EventManager.Presentation
```

Приложение будет доступно на `https://localhost:5001` и `http://localhost:5000`.

Swagger UI доступен на `/swagger`.

## Конфигурация

Строка подключения к БД читается из `ConnectionStrings:DefaultConnection`. В `appsettings.json` пароль оставлен пустым; для локальной разработки задайте полный connection string через `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` или переменную окружения `ConnectionStrings__DefaultConnection`.

### JWT

JWT использует секцию `TokenSettings`:

```json
{
  "TokenSettings": {
    "Secret": "",
    "Issuer": "EventManagerApi",
    "Audience": "EventManagerApi",
    "LifeTimeInMinutes": 15
  }
}
```

Для локальной разработки задайте секрет через User Secrets:

```bash
dotnet user-secrets set "TokenSettings:Secret" "your-development-secret-at-least-32-bytes-long" --project EventManager.Presentation
```

В production используйте уникальный длинный секрет из переменной окружения `TokenSettings__Secret` или внешнего secret storage. Не храните production-секрет в репозитории и замените его, если он был опубликован.

### Роли и доступ

- `User` может просматривать мероприятия, создавать брони и отменять только собственные брони.
- `Admin` может создавать, изменять и удалять мероприятия, а также отменять бронь любого пользователя.
- `POST /auth/register` и `POST /auth/login` доступны без JWT.
- `POST /events/{id}/book`, `GET /bookings/{id}` и `DELETE /bookings/{id}` требуют JWT.
- `POST /events`, `PUT /events/{id}` и `DELETE /events/{id}` требуют роль `Admin`.

### Получение JWT через Swagger

1. Запустите приложение в окружении `Development` и откройте `/swagger`.
2. Вызовите `POST /auth/register` с логином, паролем и ролью `User` или `Admin`.
3. Вызовите `POST /auth/login` с теми же учетными данными и скопируйте значение `token` из ответа.
4. Нажмите `Authorize` в Swagger и вставьте только JWT, без префикса `Bearer`.
5. Swagger автоматически добавит заголовок `Authorization: Bearer <token>` к защищенным запросам.

## API Endpoints

### Аутентификация

**Базовый путь**: `/auth`

- `POST /auth/register` — регистрация пользователя
  - Доступ: без токена
  - Тело: `CreatingUserBodyDTO` с полями `Login`, `Password`, `Role` (`"User"` по умолчанию, можно передать `"Admin"`)
  - Ответ: `204 No Content`
  - Ошибки: `400 Bad Request` при невалидных данных, `409 Conflict` при занятом логине

- `POST /auth/login` — авторизация пользователя
  - Доступ: без токена
  - Тело: `LogingUserDTO` с полями `Login`, `Password`
  - Ответ: `200 OK` с `UserTokenResult` (`{ "token": "..." }`)
  - Ошибка: `400 Bad Request` при неверных учётных данных

### Мероприятия

**Базовый путь**: `/events`

- `GET /events` — получить список мероприятий с фильтрацией и пагинацией
  - Параметры: `title`, `from`, `to`, `page` (по умолчанию 1), `pageSize` (по умолчанию 10)
  - Ответ: `200 OK` с `PaginatedResponseDTO<EventResponseDTO>`

- `GET /events/{id}` — получить мероприятие по ID
  - Ответ: `200 OK` с `EventResponseDTO` или `404 Not Found`

- `POST /events` — создать новое мероприятие
  - Тело: `CreateEventDTO` с полями `Title`, `Description`, `StartAt`, `EndAt`, `TotalSeats`
  - Ответ: `201 Created`
  - Ошибка валидации: `400 Bad Request`

- `PUT /events/{id}` — обновить мероприятие
  - Тело: `CreateEventDTO`
  - Ответ: `200 OK` или `404 Not Found`

- `DELETE /events/{id}` — удалить мероприятие
  - Ответ: `204 No Content` или `404 Not Found`

- `POST /events/{id}/book` — создать бронирование
  - Ответ: `202 Accepted` с `BookingDTO`
  - Если нет мест: `409 Conflict`
  - Если не найдено: `404 Not Found`

### Бронирования

**Базовый путь**: `/bookings`

- `GET /bookings/{id}` — получить бронирование по ID
  - Ответ: `200 OK` с `BookingDTO` или `404 Not Found`

- `DELETE /bookings/{id}` — отменить бронирование
  - Ответ: `204 No Content`
  - Ошибки: `403 Forbidden` для чужой брони, `404 Not Found`, `409 Conflict` для завершенной отмены или отклоненной брони

## Модели данных

**Модель Event:**

- `Id` — GUID
- `Title` — название (обязательное)
- `Description` — описание (опционально)
- `StartAt` — дата начала
- `EndAt` — дата окончания
- `TotalSeats` — общее количество мест
- `AvailableSeats` — свободные места

**Статусы бронирования:**
| Статус | Описание |
|--------|---------|
| `Pending` | Ожидает подтверждения |
| `Confirmed` | Подтверждено |
| `Rejected` | Отклонено |
| `Cancelled` | Отменено пользователем или администратором |

## Архитектура (Clean Architecture)

Проект состоит из четырёх независимых слоёв, каждый с чётко определённой ответственностью:

```
┌─────────────────────────────────────────┐
│  EventManager.Presentation              │
│  (контроллеры, HTTP слой)               │
├─────────────────────────────────────────┤
│  EventManager.Application               │
│  (бизнес-логика, сервисы,DTO)          │
├─────────────────────────────────────────┤
│  EventManager.Infrastructure            │
│  (репозитории, контекст БД, миграции)   │
├─────────────────────────────────────────┤
│  EventManager.Domain                    │
│  (сущности, исключения, общие типы)     │
└─────────────────────────────────────────┘
```

**Правила взаимодействия:**

- Presentation → Application → Infrastructure → Domain
- Domain не зависит ни от чего
- Infrastructure зависит только от Domain и Application
- Application использует интерфейсы для работы с данными

### 1. EventManager.Domain

**Назначение:** Определяет основные сущности и бизнес-правила, независимые от фреймворка и БД.

**Содержимое:**

- **Models/** — основные сущности
  - `Event.cs` — мероприятие с методом `TryReserveSeats()`
  - `Booking.cs` — бронирование
  - `PaginatedResult.cs` — результат пагинированного запроса
- **Exceptions/** — пользовательские исключения
  - `EventException` — ошибки мероприятий
  - `BookingException` — ошибки бронирований
  - `NoAvailableSeatsException` — отсутствие свободных мест
- **Common/** — константы и enum'ы
  - `BookingStatus` — статусы бронирования (Pending, Confirmed, Rejected)
  - `AppConstants` — константы приложения

**Зависимости:** нет (чистый POCO код)

### 2. EventManager.Application

**Назначение:** Содержит бизнес-логику, использует интерфейсы для абстракции от реализации.

**Содержимое:**

- **DTOs/** — объекты передачи данных между слоями
  - `CreateEventDTO` — для создания мероприятия
  - `EventInfoDTO` — для возврата информации о мероприятии
  - `BookingDTO` — для возврата информации о бронировании

- **Interfaces/** — контракты для репозиториев
  - `IEventRepository` — интерфейс доступа к мероприятиям
  - `IBookingRepository` — интерфейс доступа к бронированиям

- **Services/** — бизнес-логика
  - `EventService` — операции с мероприятиями (создание, обновление, фильтрация)
  - `BookingService` — логика бронирования, проверка свободных мест
  - `ValidationService` — валидация данных

**Зависимости:** EventManager.Domain

### 3. EventManager.Infrastructure

**Назначение:** Реализует доступ к данным, конфигурацию БД и миграции EF Core.

**Содержимое:**

- **DataAccess/** — конфигурация БД
  - `AppDbContext` — контекст Entity Framework
  - `Configuration/` — конфигурация маппирования сущностей
- **Repositories/** — реализация интерфейсов репозиториев
  - `EventRepository` — доступ к Event в БД
  - `BookingRepository` — доступ к Booking в БД

- **Migrations/** — миграции EF Core (автосгенерированные)
  - `[Timestamp]_InitialCreate.cs` — исходная схема БД
  - `AppDbContextModelSnapshot.cs` — снимок текущей модели

- **DependencyInjection.cs** — регистрация сервисов в DI контейнер

**Зависимости:** EventManager.Domain, EventManager.Application

### 4. EventManager.Presentation

**Назначение:** HTTP слой, контроллеры, middleware, точка входа приложения.

**Содержимое:**

- **Controllers/** — обработчики HTTP запросов
  - `EventsController` — endpoints для мероприятий
  - `BookingsController` — endpoints для бронирований

- **Middleware/** — обработка пипелайна ASP.NET Core
  - `ErrorHandlingMiddleware` — глобальная обработка исключений

- **BackgroundServices/** — фоновые сервисы
  - `BookingConfirmationService` — периодическое подтверждение бронирований

- **Program.cs** — конфигурация приложения и DI контейнера

- **appsettings.json** / **appsettings.Development.json** — конфигурация (строка подключения, логирование)

**Зависимости:** EventManager.Domain, EventManager.Application, EventManager.Infrastructure

## Бизнес-логика

### Сценарий использования

1. `POST /events` — создать мероприятие
2. `POST /events/{id}/book` — создать бронирование (статус: `Pending`)
3. `GET /bookings/{bookingId}` — проверить статус
4. Фоновый сервис подтверждает бронирования каждые 15 секунд (статус: `Confirmed` или `Rejected`)

### Синхронизация и защита от гонок

Для предотвращения условий гонки при одновременных бронированиях используются:

- **`lock`** — в `BookingService` для атомарного уменьшения `AvailableSeats`
- **`SemaphoreSlim`** — в `BookingConfirmationService` для последовательной обработки подтверждений

Это гарантирует консистентность данных при параллельных запросах.

### Обработка конфликтов

Если на мероприятии нет свободных мест, API возвращает `409 Conflict`.

Пример при одновременных бронированиях:

1. На событии осталось 1 место
2. Клиент A и B одновременно бронируют
3. Первый получает `202 Accepted`, второй — `409 Conflict`

## Тестирование

Проект содержит два набора тестов:

### Модульные тесты (EventManager.Tests)

Тестирование сервисов и репозиториев на In-Memory БД (Docker не требуется).

```bash
dotnet test EventManager.Tests/EventManager.Tests.csproj
```

Инструменты: `xunit`, `FluentAssertions`, `Moq`, `Microsoft.EntityFrameworkCore.InMemory`

### Интеграционные тесты (EventManager.IntegrationTests)

Тестирование репозиториев с реальным PostgreSQL (требуется Docker).

```bash
dotnet test EventManager.IntegrationTests/EventManager.IntegrationTests.csproj
```

Инструменты: `Testcontainers.PostgreSql` (поднимает временный контейнер)

### Запуск всех тестов

```bash
dotnet test EventManager.sln
```

## Миграции базы данных

Миграции находятся в [EventManager.Infrastructure/Migrations/](EventManager.Infrastructure/Migrations/).

При старте приложения они применяются автоматически.

### Создание новой миграции

```bash
dotnet tool install --global dotnet-ef  # если еще не установлен

dotnet ef migrations add <MigrationName> \
  --project EventManager.Infrastructure \
  --startup-project EventManager.Presentation
```

### Применение миграций

```bash
dotnet ef database update \
  --project EventManager.Infrastructure \
  --startup-project EventManager.Presentation
```

## Запуск в IDE

- **Visual Studio** или **Rider**: откройте `EventManager.sln`
- Установите `EventManager.Presentation` как стартовый проект
- Нажмите F5
