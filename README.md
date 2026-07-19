# Barberos — онлайн-запись в барбершоп

Веб-приложение для записи клиентов в барбершоп. См. подробный план в [PLAN.md](PLAN.md).

**Стек:** .NET 10 (ASP.NET Core Web API) · React + TypeScript (Vite) · PostgreSQL

## Структура репозитория

```
├── src/
│   ├── Barberos.Domain/          # Сущности и доменные правила
│   ├── Barberos.Application/     # Use cases, DTO, интерфейсы (ISmsSender, IAppDbContext)
│   ├── Barberos.Infrastructure/  # EF Core, PostgreSQL, реализации сервисов
│   └── Barberos.Api/             # ASP.NET Core Web API
├── tests/
│   ├── Barberos.UnitTests/
│   └── Barberos.IntegrationTests/
├── frontend/                     # React SPA (Vite + TypeScript)
├── Barberos.slnx
└── docker-compose.yml
```

## Требования

- .NET 10 SDK
- Node.js 20+ (проверено на 24)
- PostgreSQL 17 (или через Docker)

## Быстрый старт (локально)

1. Поднять PostgreSQL:
   ```bash
   docker compose up -d postgres
   ```

2. Backend (из корня):
   ```bash
   dotnet run --project src/Barberos.Api
   ```
   API: `http://localhost:5080`, health: `http://localhost:5080/health`, OpenAPI: `/openapi/v1.json`

3. Frontend:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
   SPA: `http://localhost:5173` (запросы `/api` проксируются на backend)

## Запуск всего стека в Docker

```bash
docker compose up --build
```

- Frontend: `http://localhost:5173`
- API: `http://localhost:5080`
- PostgreSQL: `localhost:5432`

## Миграции EF Core

Установить инструмент (однократно):
```bash
dotnet tool install --global dotnet-ef
```

Создать и применить миграцию:
```bash
dotnet ef migrations add InitialCreate \
  --project src/Barberos.Infrastructure \
  --startup-project src/Barberos.Api

dotnet ef database update \
  --project src/Barberos.Infrastructure \
  --startup-project src/Barberos.Api
```

> Защиту от двойного бронирования (EXCLUDE-constraint по времени мастера) добавить в миграцию вручную —
> см. комментарий в `BookingConfiguration`.

## Конфигурация

Секреты (JWT-ключ, строка подключения, SMS-провайдер) в проде — через переменные окружения
или user-secrets, не в `appsettings.json`. Ключевые секции: `ConnectionStrings:Postgres`, `Jwt`, `Cors`, `Barbershop`.

## Тесты

```bash
dotnet test
```
