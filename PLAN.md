# План проекта: Онлайн-бронирование в барбершоп

> **Стек:** .NET 10 (ASP.NET Core Web API) · React (SPA) · PostgreSQL
> **Модель:** один барбершоп (клиенты + мастера + админ)
> **Оплата в MVP:** на месте (онлайн-оплата — в бэклоге)
> **Дата составления:** 2026-07-18

---

## 1. Обзор и цели

Веб-приложение для одного барбершопа, позволяющее клиентам записываться к мастерам онлайн, а сотрудникам — управлять расписанием и записями.

**Ключевые ценности MVP:**
- Клиент выбирает услугу → мастера → свободный слот → подтверждает бронь по SMS.
- Мастера ведут своё расписание (часы работы, перерывы, отпуска).
- Автоматические уведомления (подтверждение, напоминание, отмена).
- Отзывы и рейтинги мастеров/услуг после визита.

**Не входит в MVP (бэклог):** онлайн-оплата, программа лояльности, мультифилиальность, мобильное нативное приложение.

---

## 2. Роли и права доступа

| Роль | Возможности |
|------|-------------|
| **Гость** | Просмотр услуг, мастеров, свободных слотов; регистрация/вход |
| **Клиент** | Создание/отмена/перенос своих броней; просмотр истории; оставление отзыва |
| **Мастер** | Управление своим расписанием; просмотр своих записей; отметка «выполнено/не пришёл» |
| **Админ** | Управление услугами, мастерами, всеми бронями; просмотр аналитики; модерация отзывов |

Реализация: JWT + role-based authorization (`[Authorize(Roles=...)]` / policy-based).

---

## 3. Аутентификация: телефон + SMS-код

Поток:
1. Пользователь вводит номер телефона.
2. Backend генерирует OTP (6 цифр), сохраняет хеш кода + TTL (например, 5 мин), отправляет через SMS-провайдера.
3. Пользователь вводит код → backend проверяет → выдаёт **access token (JWT, ~15 мин)** + **refresh token (в httpOnly cookie / БД, ~30 дней)**.
4. При первом входе создаётся профиль (имя запрашивается после верификации).

**Защита:**
- Rate limiting на отправку OTP (по номеру и по IP).
- Лимит попыток ввода кода (например, 5), затем блокировка кода.
- Anti-fraud: cooldown между запросами кода.

**SMS-провайдер (уточнить регион):** Twilio / Eskiz / Play Mobile / SMS.ru. Абстрагировать за интерфейсом `ISmsSender`, чтобы легко менять.

---

## 4. Архитектура backend (Clean / Layered монолит, .NET 10)

```
src/
├── Barberos.Domain/          # Сущности, value objects, доменные правила. Без зависимостей.
├── Barberos.Application/     # Use cases, DTO, интерфейсы (ISmsSender, IEmailSender), валидация.
├── Barberos.Infrastructure/  # EF Core, PostgreSQL, реализации сервисов (SMS, уведомления), миграции.
└── Barberos.Api/             # ASP.NET Core Web API: контроллеры, middleware, DI, auth, Swagger.

tests/
├── Barberos.UnitTests/
└── Barberos.IntegrationTests/  # Testcontainers + PostgreSQL
```

**Ключевые технологии:**
- ASP.NET Core Web API (.NET 10)
- EF Core 10 + Npgsql (PostgreSQL provider)
- FluentValidation — валидация входных данных
- MediatR (опционально) — разделение команд/запросов
- Serilog — структурированное логирование
- Swagger / OpenAPI — документация API
- Hangfire или BackgroundService — фоновые задачи (напоминания)

**Принципы:**
- Все операции с бронями — в транзакциях, с проверкой конфликтов слотов на уровне БД (unique constraint / advisory lock) во избежание двойного бронирования.
- Время хранится в UTC, отображается в таймзоне барбершопа.

---

## 5. Модель данных (PostgreSQL)

Основные таблицы:

- **users** — `id, phone (unique), name, role, created_at`
- **otp_codes** — `id, phone, code_hash, expires_at, attempts, used`
- **refresh_tokens** — `id, user_id, token_hash, expires_at, revoked`
- **services** — `id, name, description, duration_minutes, price, is_active`
- **masters** — `id, user_id (nullable), name, bio, photo_url, is_active`
- **master_services** — связь мастер↔услуга (кто какую услугу оказывает) `many-to-many`
- **schedules** — рабочие часы мастера `id, master_id, day_of_week, start_time, end_time`
- **time_off** — отпуска/перерывы `id, master_id, start_at, end_at, reason`
- **bookings** — `id, client_id, master_id, service_id, start_at, end_at, status, created_at`
  - `status`: `pending / confirmed / completed / cancelled / no_show`
  - **Constraint** от двойного бронирования: exclusion constraint по `(master_id, tstzrange(start_at, end_at))`
- **reviews** — `id, booking_id (unique), client_id, master_id, rating (1-5), comment, created_at, is_published`
- **notifications** — `id, user_id, type, channel, payload, status, sent_at`

Диаграмма (упрощённо):
```
users 1─* bookings *─1 services
              │
              *─1 masters ─* master_services
                     │
                     ├─* schedules
                     └─* time_off
bookings 1─1 reviews
```

---

## 6. Логика расчёта свободных слотов

Ключевой алгоритм MVP. При запросе слотов для `(мастер, услуга, дата)`:

1. Взять рабочие часы мастера на день недели (`schedules`).
2. Вычесть периоды `time_off`.
3. Вычесть существующие `bookings` (в статусах, занимающих слот).
4. Разбить оставшиеся интервалы на слоты с шагом (напр. 15 мин), длина слота = `service.duration_minutes`.
5. Отфильтровать прошедшее время + буфер до записи.

Возвращать только реально доступные слоты. Финальная проверка доступности — снова при создании брони (внутри транзакции), т.к. кто-то мог занять слот.

---

## 7. Уведомления

**Каналы MVP:** SMS (обязательно, т.к. вход по телефону) + опционально Telegram/email.

**Триггеры:**
| Событие | Кому | Когда |
|---------|------|-------|
| Бронь создана | Клиент + мастер | сразу |
| Напоминание | Клиент | за 24 ч и/или 2 ч до визита |
| Отмена/перенос | Клиент + мастер | сразу |
| Запрос отзыва | Клиент | после `completed` |

**Реализация:** очередь уведомлений в таблице `notifications`, фоновый воркер (`BackgroundService`/Hangfire) с расписанием обрабатывает и отправляет, ретраи при ошибках.

---

## 8. Frontend (React SPA)

**Стек:**
- React + TypeScript + Vite
- React Router — маршрутизация
- TanStack Query (React Query) — запросы к API, кэширование
- Zustand / Redux Toolkit — состояние (авторизация)
- React Hook Form + Zod — формы и валидация
- Tailwind CSS / shadcn/ui — UI
- Axios/fetch с интерцептором для refresh-токена

**Экраны:**

*Публичные:*
- Главная (о барбершопе, услуги, мастера)
- Каталог услуг
- Страница мастера (профиль, отзывы, рейтинг)
- Флоу бронирования: услуга → мастер → дата/слот → SMS-верификация → подтверждение

*Клиент:*
- Мои записи (активные/история)
- Отмена/перенос брони
- Оставить отзыв

*Мастер:*
- Дашборд с записями (день/неделя)
- Управление расписанием (часы, перерывы, отпуска)
- Отметка статуса записи

*Админ:*
- Управление услугами
- Управление мастерами и их услугами
- Все брони (календарь/список)
- Модерация отзывов
- Базовая аналитика (кол-во записей, загрузка мастеров, доход)

---

## 9. API (черновой контракт, REST)

```
# Auth
POST   /api/auth/request-otp        { phone }
POST   /api/auth/verify-otp         { phone, code } → tokens
POST   /api/auth/refresh
POST   /api/auth/logout

# Services
GET    /api/services
POST   /api/services                (admin)
PUT    /api/services/{id}           (admin)

# Masters
GET    /api/masters
GET    /api/masters/{id}
POST   /api/masters                 (admin)

# Availability
GET    /api/availability?masterId=&serviceId=&date=

# Bookings
POST   /api/bookings                (client)
GET    /api/bookings/my             (client)
PATCH  /api/bookings/{id}/cancel
PATCH  /api/bookings/{id}/reschedule
PATCH  /api/bookings/{id}/status    (master/admin)

# Schedule
GET    /api/masters/{id}/schedule
PUT    /api/masters/{id}/schedule   (master/admin)
POST   /api/masters/{id}/time-off

# Reviews
POST   /api/reviews                 (client)
GET    /api/masters/{id}/reviews
PATCH  /api/reviews/{id}/moderate   (admin)
```

---

## 10. Нефункциональные требования

- **Безопасность:** HTTPS, hashing OTP/токенов, rate limiting, защита от IDOR (проверка владения бронью), CORS whitelist, secrets в переменных окружения/секрет-менеджере.
- **Валидация:** на клиенте (UX) и на сервере (истина).
- **Логирование/мониторинг:** Serilog + структурированные логи; health checks (`/health`).
- **Таймзоны:** UTC в БД, таймзона барбершопа в конфиге.
- **Тесты:** unit (доменная логика, расчёт слотов), integration (API + Testcontainers Postgres).
- **Производительность:** индексы на `bookings(master_id, start_at)`, пагинация списков.

---

## 11. Инфраструктура и деплой

- **Контейнеризация:** Docker + docker-compose (api, postgres, frontend, nginx).
- **CI/CD:** GitHub Actions — build, test, миграции, деплой.
- **Reverse proxy:** Nginx (статика frontend + проксирование API).
- **Миграции:** EF Core migrations, применяются при деплое.
- **Окружения:** dev / staging / prod, конфиги через `appsettings.{env}.json` + env vars.
- **Бэкапы:** регулярные дампы PostgreSQL.

---

## 12. Дорожная карта (по этапам)

### Этап 0 — Подготовка (0.5 нед) — ✅ выполнено
- [x] Инициализация репозитория, структура solution (.NET 10): Domain / Application / Infrastructure / Api + tests
- [x] Настройка React + Vite проекта (роутер, react-query, zustand, axios, tailwind, заглушки страниц)
- [x] docker-compose (Postgres + API + frontend) + Dockerfile'ы + nginx.conf
- [ ] CI: build + test _(GitHub Actions — не настроен)_

### Этап 1 — Ядро и auth (1–1.5 нед)
- [x] Домен (11 сущностей + enums), EF Core (DbContext + конфигурации), миграция `InitialCreate` сгенерирована
  - [ ] применить миграцию к БД (`dotnet ef database update`)
  - [ ] добавить EXCLUDE-constraint от двойного бронирования вручную в миграцию
- [ ] Аутентификация по SMS (OTP, JWT, refresh) _(сущности готовы, логика — нет)_
- [ ] Интеграция SMS-провайдера (за интерфейсом) _(есть `ISmsSender` + заглушка `ConsoleSmsSender`)_
- [ ] Роли и авторизация _(enum `UserRole` есть, policy/middleware — нет)_

### Этап 2 — Каталог и расписание (1 нед)
- [ ] CRUD услуг и мастеров (админ)
- [ ] Управление расписанием мастеров, time-off
- [ ] Алгоритм расчёта свободных слотов + endpoint availability

### Этап 3 — Бронирование (1–1.5 нед)
- [ ] Создание брони (транзакция, защита от двойного бронирования)
- [ ] Отмена/перенос
- [ ] Личный кабинет клиента, кабинет мастера
- [ ] Статусы броней

### Этап 4 — Уведомления (0.5–1 нед)
- [ ] Таблица + воркер уведомлений
- [ ] Триггеры: подтверждение, напоминание, отмена, запрос отзыва

### Этап 5 — Отзывы и админка (1 нед)
- [ ] Отзывы и рейтинги, модерация
- [ ] Админ-аналитика (базовая)

### Этап 6 — Полировка и релиз (0.5–1 нед)
- [ ] E2E-тесты ключевых флоу
- [ ] Аудит безопасности
- [ ] Деплой на staging → prod

**Ориентировочно MVP:** ~6–8 недель для одного разработчика full-stack.

---

## 13. Бэклог (после MVP)

- Онлайн-оплата / депозит (Stripe / ЮKassa / Click / Payme) — архитектурно точка расширения в `bookings` (статус оплаты) и отдельный `payments`.
- Программа лояльности, промокоды, абонементы
- Мультифилиальность (сеть)
- Мобильное приложение / PWA с push
- Интеграция с Google Calendar мастеров
- Расширенная аналитика и отчёты

---

## 14. Открытые вопросы (уточнить перед стартом)

1. Регион и SMS-провайдер (влияет на стоимость и интеграцию).
2. Разрешён ли перенос брони клиентом или только отмена?
3. Нужен ли гостевой заказ (бронь без регистрации, только по номеру) или обязателен вход?
4. Политика отмены (за сколько часов можно отменить бесплатно)?
5. Один слот = один мастер, или есть общие ресурсы (кресла)?
