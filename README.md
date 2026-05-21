# Event Manager — backend

**Описание**

- **Проект**: ASP.NET Core Web API для управления мероприятиями и бронированиями.
- **Функции**: получение, фильтрация, пагинация, создание, обновление, удаление и валидация мероприятий.
- **Дополнительно**: система бронирования с автоматическим подтверждением через фоновый сервис, глобальная обработка ошибок через middleware и модульное покрытие сервисов.

**Требования**

- **.NET SDK**: установите .NET 10 (проверить: `dotnet --version`, ожидается `10.x`).

**Как запустить локально**

- Откройте терминал в корне репозитория и выполните:

```
cd EventManager
dotnet restore
dotnet build
dotnet run
```

- По умолчанию приложение запустится на локальных адресах (см. вывод консоли), обычно `https://localhost:5001` и `http://localhost:5000`.
- В среде разработки включён Swagger UI — откройте `/swagger` для интерактивной документации.

**API Endpoints**

**Мероприятия**

- **Базовый путь**: `/events`

- `GET /events`

  - Получить список всех мероприятий с фильтрацией и пагинацией
  - Параметры фильтрации: `title`, `from`, `to`
  - Параметры пагинации: `page` (по умолчанию 1), `pageSize` (по умолчанию 10)
  - Возвращает: `200 OK` с `PaginatedResultDTO<Event>`

- `GET /events/{id}`

  - Получить мероприятие по ID
  - Возвращает: `200 OK` с `EventDTO` или `404 Not Found`

- `POST /events`

  - Создать новое мероприятие
  - Тело запроса: `EventDTO` с `Title`, `Description`, `StartAt`, `EndAt`
  - Возвращает: `201 Created`
  - Ошибка валидации: `400 Bad Request`

- `PUT /events/{id}`

  - Обновить существующее мероприятие
  - Тело запроса: `EventDTO`
  - Возвращает: `200 OK` или `404 Not Found`
  - Применяется такая же валидация, как при создании

- `DELETE /events/{id}`
  - Удалить мероприятие
  - Возвращает: `204 No Content` или `404 Not Found`

**Бронирования**

- **Базовый путь**: `/bookings`

- `GET /bookings/{id}`
  - Получить бронирование по ID
  - Возвращает: `200 OK` с `BookingDTO` или `404 Not Found`

**Статусы бронирований**

| Статус      | Описание                                         |
| ----------- | ------------------------------------------------ |
| `Pending`   | Бронирование создано, ожидает подтверждения      |
| `Confirmed` | Бронирование автоматически подтверждено сервисом |
| `Canceled`  | Бронирование отменено                            |

**Особенности реализации**

- **Контроллеры**:

  - `EventsController` — реализует CRUD операции для мероприятий и запросы с фильтрацией/пагинацией
  - `BookingsController` — управляет бронированиями событий

- **Сервисы бизнес-логики**:

  - `EventService` — содержит основную логику для работы с мероприятиями, обрабатывает ошибки через `EventException`
  - `BookingService` — содержит логику для работы с бронированиями, обрабатывает ошибки через `BookingException`
  - `ValidationService` — проверяет `EventDTO` на корректность заголовка, дат и диапазона

- **Хранилище данных**:

  - `IEventRepository` / `EventRepository` — абстрагирует работу с данными мероприятий
  - `IBookingRepository` / `BookingRepository` — абстрагирует работу с данными бронирований

- **Фоновые сервисы**:

  - `BookingConfirmationService` — периодически проверяет бронирования со статусом `Pending` и автоматически подтверждает их

- **Обработка ошибок**:
  - `ErrorHandlingMiddleware` — глобальная обработка исключений, возвращает JSON `ProblemDetails` для всех ошибок

**Тесты**

Проект `EventManager.Tests` содержит юнит-тесты для всех компонентов:

- `EventServiceTests` — проверка фильтрации, пагинации, получения по ID, создания, обновления и удаления мероприятий
- `BookingServiceTests` — проверка управления бронированиями и изменения статусов
- `BookingRepositoryTests` — проверка работы хранилища бронирований
- `BookingTests` — проверка модели `Booking` и переходов между статусами
- `ValidationServiceTests` — проверка правил валидации `EventDTO`

В тестах используются: `Moq`, `FluentAssertions`, `Xunit`

**Структура репозитория**

```
EventManager/
├── Controllers/              # Контроллеры API (EventsController, BookingsController)
├── Services/
│   ├── EventService/         # Бизнес-логика мероприятий
│   ├── BookingService/       # Бизнес-логика бронирований
│   └── ValidationService/    # Валидация данных
├── Data/
│   ├── EventRepository/      # Хранилище мероприятий
│   └── BookingRepository/    # Хранилище бронирований
├── Models/                   # Модели и DTO (Event, Booking, PaginatedResult)
├── Common/                   # Общие типы (BookingStatus)
├── Exceptions/               # Пользовательские исключения (EventException, BookingException)
├── Middleware/               # Глобальная обработка ошибок
└── BackgroundServices/       # Фоновые сервисы (BookingConfirmationService)

EventManager.Tests/
└── Services/                 # Модульные тесты для всех компонентов
```

**Запуск в IDE**

- Откройте решение `EventManager.sln` или проект `EventManager/EventManager.csproj` в Visual Studio или Rider.
- Установите `EventManager` как стартовый проект и запустите (F5).

**Запуск тестов**

- Выполните:

```
cd EventManager.Tests
dotnet test
```
