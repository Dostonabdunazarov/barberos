using Barberos.Api.Auth;
using Barberos.Api.Middleware;
using Barberos.Api.RateLimiting;
using Barberos.Api.Validation;
using Barberos.Application.Abstractions;
using Barberos.Application.Services;
using Barberos.Infrastructure;
using Barberos.Infrastructure.Storage;
using Barberos.Infrastructure.Auth;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Логирование — Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Controllers + OpenAPI / Swagger
builder.Services.AddControllers(options =>
    options.Filters.Add<ValidationFilter>());
builder.Services.AddOpenApi();

// FluentValidation: регистрируем валидаторы из Application-сборки
builder.Services.AddValidatorsFromAssemblyContaining<CreateServiceRequestValidator>();

// CORS для React SPA.
// В Production origins ОБЯЗАТЕЛЬНЫ (fail-closed): при пустом Cors:Origins стартап падает,
// чтобы не задеплоить неверную политику. В dev — дефолт на локальный Vite.
const string CorsPolicy = "spa";
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
if (corsOrigins.Length == 0)
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException(
            "Cors:Origins не задан. В Production укажите реальные origin(ы) SPA через Cors__Origins__0 и т.д.");
    corsOrigins = ["http://localhost:5173"];
}
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// За обратным прокси (nginx) читаем реальный IP клиента и схему из X-Forwarded-*,
// иначе rate limiting по IP и HTTPS-редирект работают неверно.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Прокси в нашей сети — доверяем всем (в контейнерной сети адрес прокси не фиксирован).
    // При деплое можно сузить до KnownProxies/KnownNetworks конкретного nginx.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Инфраструктура (EF Core + Postgres + auth-сервисы)
builder.Services.AddInfrastructure(builder.Configuration);

// Файловое хранилище загрузок (фото работ мастеров) — на локальном диске в wwwroot.
// WebRootPath может быть null, если папки ещё нет: берём стандартный путь и создаём.
var webRoot = builder.Environment.WebRootPath
    ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(webRoot);
builder.Services.AddScoped<IFileStorage>(_ =>
    new LocalFileStorage(new LocalFileStorageOptions { RootPath = webRoot }));

// Аутентификация/авторизация (JWT bearer + policies)
builder.Services.AddApiAuth();

// Rate limiting (перебор пароля на login, спам на публичном создании брони)
builder.Services.AddApiRateLimiting(builder.Configuration);

// Обработка доменных исключений. Порядок важен: каждый handler возвращает false
// для «не своих» исключений, передавая их следующему.
builder.Services.AddExceptionHandler<AuthExceptionHandler>();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres");

var app = builder.Build();

// Применение миграций при старте (можно отключить через Database:AutoMigrate=false,
// если миграции накатываются отдельным шагом деплоя). Должно идти ДО бутстрапа админа,
// т.к. бутстрап читает таблицу Users.
using (var scope = app.Services.CreateScope())
{
    // Форсируем валидацию критичных опций (Jwt:Key и т.д.) ДО работы с БД:
    // ValidateOnStart иначе срабатывает только на app.Run(), уже после миграций/бутстрапа.
    _ = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<Barberos.Infrastructure.Auth.JwtOptions>>()
        .Value;

    if (app.Configuration.GetValue("Database:AutoMigrate", true))
    {
        var db = scope.ServiceProvider.GetRequiredService<Barberos.Infrastructure.Persistence.AppDbContext>();
        await db.Database.MigrateAsync();
    }

    // Бутстрап первого админа (если админов нет и заданы Bootstrap:Admin:*).
    var bootstrapper = scope.ServiceProvider.GetRequiredService<AdminBootstrapper>();
    await bootstrapper.EnsureAdminAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Первым — восстановление реального клиента/схемы из заголовков прокси.
app.UseForwardedHeaders();

// HSTS только вне dev (иначе ломает localhost по http).
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSecurityHeaders();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
// Раздача загруженных файлов (фото работ) из wwwroot — публично, до авторизации.
app.UseStaticFiles();
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Barberos API is running");

app.Run();
