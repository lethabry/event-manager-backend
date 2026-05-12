# Event Manager — backend

**Описание**

- **Проект**: ASP.NET Core Web API для управления мероприятиями.
- **Функции**: получение, фильтрация, пагинация, создание, обновление, удаление и валидация мероприятий.
- **Дополнительно**: глобальная обработка ошибок через middleware и модульное покрытие сервисов.

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

**API**

- **Базовый путь**: `/events`

- `GET /events`

  - Фильтрация: `title`, `from`, `to`
  - Пагинация: `page` (по умолчанию 1), `pageSize` (по умолчанию 10)
  - Возвращает `PaginatedResultDTO<Event>`.

- `GET /events/{id}`

  - Возвращает мероприятие по `id` или `404 Not Found`.

- `POST /events`

  - Принимает `EventDTO` с `Title`, `Description`, `StartAt`, `EndAt`
  - Создаёт событие и возвращает `201 Created`
  - Ошибки валидации дают `400 Bad Request`.

- `PUT /events/{id}`

  - Обновляет мероприятие по `id`
  - Возвращает `404 Not Found`, если событие не найдено
  - Применяет такую же валидацию, как `POST`.

- `DELETE /events/{id}`
  - Удаляет мероприятие и возвращает `204 No Content`.

**Особенности реализации**

- `EventsController` реализует CRUD и запросы с фильтрацией/пагинацией.
- `EventService` содержит бизнес-логику и обрабатывает ошибки через `EventException`.
- `ValidationService` проверяет `EventDTO` на наличие заголовка, дат и корректный диапазон.
- `ErrorHandlingMiddleware` возвращает JSON `ProblemDetails` для ошибок.
- `IEventRepository` абстрагирует хранилище данных.

**Тесты**

- Проект `EventManager.Tests` содержит юнит-тесты для сервисов:
  - `EventServiceTests` — проверка фильтрации, пагинации, получения по id, создания, обновления и удаления мероприятий.
  - `ValidationServiceTests` — проверка правил валидации `EventDTO`.
- В тестах используются `Moq`, `FluentAssertions` и `Xunit`.

**Структура репозитория**

- `EventManager/Controllers` — контроллеры API
- `EventManager/Services` — бизнес-логика и валидации
- `EventManager/Data` — репозиторий данных
- `EventManager/Models` — модели и DTO
- `EventManager/Middleware` — глобальная обработка ошибок
- `EventManager.Tests` — модульные тесты

**Запуск в IDE**

- Откройте решение `EventManager.sln` или проект `EventManager/EventManager.csproj` в Visual Studio или Rider.
- Установите `EventManager` как стартовый проект и запустите (F5).

**Запуск тестов**

- Выполните:

```
cd EventManager.Tests
dotnet test
```
