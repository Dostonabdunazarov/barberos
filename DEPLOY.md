# Деплой Barberos на VPS (Docker Compose)

Продовый стек: **PostgreSQL + API (.NET 10) + Frontend (nginx со статикой SPA) + nginx-прокси с TLS**.
Наружу открыт только прокси (порты 80/443); остальные сервисы — во внутренней сети compose.

Файлы: [docker-compose.prod.yml](docker-compose.prod.yml), [.env.example](.env.example),
[deploy/nginx/barberos.conf](deploy/nginx/barberos.conf).

---

## 1. Предварительно

- VPS с Linux, установленные Docker и Docker Compose plugin.
- Домен, указывающий A-записью на IP сервера (напр. `barberos.example.com`).
- Открытые порты 80 и 443.

## 2. Секреты (`.env`)

```bash
cp .env.example .env
# отредактируйте .env — заполните реальные значения
```

Сгенерируйте сильные секреты:

```bash
openssl rand -base64 48   # → Jwt__Key
openssl rand -base64 24   # → POSTGRES_PASSWORD (продублируйте в ConnectionStrings__Postgres)
```

Обязательно задайте: `POSTGRES_PASSWORD`, `ConnectionStrings__Postgres`, `Jwt__Key`,
`Cors__Origins__0` (реальный https-origin SPA), `AllowedHosts` (домен),
`Bootstrap__Admin__Email` / `Bootstrap__Admin__Password`.

> API **fail-closed**: без `Cors__Origins__*` в Production он не стартует; placeholder-ключ
> `CHANGE_ME...` в `Jwt__Key` тоже отклоняется на старте.

## 3. TLS-сертификаты

Положите сертификаты в `deploy/nginx/certs/`:

- `deploy/nginx/certs/fullchain.pem`
- `deploy/nginx/certs/privkey.pem`

И замените `server_name barberos.example.com` в [deploy/nginx/barberos.conf](deploy/nginx/barberos.conf)
на свой домен.

**Let's Encrypt (certbot), первичная выдача:**

```bash
# 1) временно поднимите только прокси, отдающий ACME-челлендж по HTTP
docker compose -f docker-compose.prod.yml up -d proxy
# 2) выпустите сертификат (webroot смонтирован как certbot-www → /var/www/certbot)
docker run --rm \
  -v barberos_certbot-www:/var/www/certbot \
  -v "$(pwd)/deploy/nginx/certs:/etc/letsencrypt/live-out" \
  certbot/certbot certonly --webroot -w /var/www/certbot \
  -d barberos.example.com --email admin@barberos.example.com --agree-tos --no-eff-email
# затем скопируйте fullchain.pem/privkey.pem в deploy/nginx/certs и перезапустите proxy
```

> Для простоты можно использовать самоподписанный сертификат на этапе staging или
> вынести TLS на внешний прокси (Caddy/Traefik). Схема compose это допускает.

## 4. Запуск

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

При старте API **сам применяет миграции** (`Database:AutoMigrate=true` по умолчанию), включая
EXCLUDE-constraint от двойного бронирования, и создаёт первого админа из `Bootstrap__Admin__*`.
Чтобы накатывать миграции отдельным шагом, задайте `Database__AutoMigrate=false` и примените вручную
(`dotnet ef database update`).

## 5. Проверка

```bash
curl -fsS https://barberos.example.com/api/../health   # health API через прокси → frontend → api
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs -f api
```

Health endpoint API: `/health` (проверяет подключение к Postgres).

## 6. Обновление версии

```bash
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

## 7. Бэкапы БД

```bash
# дамп
docker compose -f docker-compose.prod.yml exec postgres \
  pg_dump -U "$POSTGRES_USER" "$POSTGRES_DB" | gzip > backup-$(date +%F).sql.gz
# восстановление
gunzip -c backup-YYYY-MM-DD.sql.gz | \
  docker compose -f docker-compose.prod.yml exec -T postgres psql -U "$POSTGRES_USER" "$POSTGRES_DB"
```

Настройте регулярный дамп по cron и хранение вне сервера.

---

## Чеклист перед prod

- [ ] `.env` заполнен, `Jwt__Key` ≥ 32 символов (реальный секрет, не placeholder)
- [ ] `Cors__Origins__0` = реальный https-origin, `AllowedHosts` = домен
- [ ] `Bootstrap__Admin__Password` — сильный; сменить при первом входе
- [ ] TLS-сертификаты на месте, `server_name` в nginx = домен
- [ ] Health `/health` отвечает `Healthy`
- [ ] Настроен бэкап PostgreSQL
- [ ] CI зелёный (см. [.github/workflows/ci.yml](.github/workflows/ci.yml))
