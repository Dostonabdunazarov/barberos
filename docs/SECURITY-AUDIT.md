# Аудит безопасности — Barberos API (Этап 5)

Дата: 2026-07-20. Область: backend (.NET 10), конфигурация, зависимости, инфраструктура деплоя.
Метод: чтение кода auth/DI/middleware, анализ зависимостей (`dotnet list package --vulnerable`),
проверка конфигов и Docker/compose.

## Итог

Кодовая база в хорошем состоянии: пароли, refresh-токены, валидация JWT и rate limiting
реализованы корректно. Этап 5 закрыл гэпы hardening для прода. Ниже — найденное и что сделано.

## Исправлено

| Severity | Проблема | Исправление |
|----------|----------|-------------|
| **High** | Транзитивный `Microsoft.OpenApi 2.0.0` — уязвимость GHSA-v5pm-xwqc-g5wc | Прямой pin `Microsoft.OpenApi 2.7.5`; в CI — gate `--vulnerable` |
| **High** (сборка) | Плавающие версии (`10.*`, `12.*`, ...) → конфликт версий EF Core сборок, невоспроизводимый билд | Все версии пакетов зафиксированы; стек EF выровнен на 10.0.4 (под Npgsql 10.0.3) |
| **Critical** | `Jwt:Key` = placeholder `CHANGE_ME...` проходил валидацию (длина 51) → риск боевого запуска с публичным ключом | Валидация отклоняет ключ на `CHANGE_ME` (`DependencyInjection.cs`); опции форсируются в `Program.cs` **до** миграций/бутстрапа, иначе `ValidateOnStart` срабатывает поздно |
| **High** | CORS при пустом `Cors:Origins` молча падал на `localhost:5173` (дефолт был и в коде, и в `appsettings.json`) | Fail-closed: в Production пустой список → исключение на старте; localhost-origin убран из base `appsettings.json`, перенесён в `appsettings.Development.json` |
| **High** | Нет HSTS | `app.UseHsts()` вне dev |
| **High** | Нет security-заголовков ответа | `SecurityHeadersMiddleware`: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `CSP`, `Permissions-Policy` |
| **Medium** | За прокси не читался реальный IP клиента (rate limiting по IP работал по адресу прокси) | `ForwardedHeaders` (X-Forwarded-For/Proto) первым в пайплайне |
| **Medium** | На чистой БД старт падал: бутстрап админа до создания схемы | Авто-миграция при старте перед бутстрапом (отключается `Database:AutoMigrate=false`) |

## Проверено — корректно, изменений не требуется

- **Хеширование паролей**: ASP.NET Core `PasswordHasher<User>` (PBKDF2-HMAC-SHA256). Открытый пароль не хранится и не логируется.
- **Refresh-токен**: 32 байта из `RandomNumberGenerator`; в БД только SHA-256 хеш; ротация при refresh; cookie `HttpOnly`+`Secure`+`SameSite=None`, `Path=/api/auth`. Смена пароля отзывает все сессии.
- **Валидация JWT**: issuer/audience/lifetime/signing key — все `true`; clock skew 30 сек.
- **Rate limiting**: login (5/мин) и booking (10/мин) по IP, 429 в ProblemDetails.
- **SQL-инъекции**: только EF LINQ (параметризовано); raw SQL — статичный DDL в миграции.
- **Утечки в логах**: пароли/токены/секреты не логируются; бутстрап логирует только email.
- **Swagger/OpenAPI**: только в Development (`MapOpenApi` под `IsDevelopment`).
- **Авторизация**: policies Staff/Admin; мастер видит только свои брони; manage-эндпоинт не отдаёт телефон гостя.
- **ManageToken**: `Guid.NewGuid()` на .NET Core+ криптографически случаен (122 бита) — пригоден как секрет; помечен как секрет в модели. Смена типа на строковый токен — возможное улучшение defense-in-depth (требует миграции схемы), не блокер.

## Обязательные env-переменные в Production

`Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `ConnectionStrings__Postgres`, `Cors__Origins__0` (обяз.),
`AllowedHosts`, `Bootstrap__Admin__Email`, `Bootstrap__Admin__Password`. См. [.env.example](../.env.example).

## Остаточные рекомендации (не блокеры)

- Рассмотреть сокращение срока refresh-токена (сейчас 30 дней) или абсолютный TTL.
- При известной топологии сети сузить `ForwardedHeaders` до `KnownProxies`/`KnownIPNetworks` конкретного nginx.
- ManageToken → выделенный строковый секрет (при следующей миграции схемы).
