using Barberos.Api.Auth;
using Barberos.Api.RateLimiting;
using Barberos.Api.Validation;
using Barberos.Application.Services;
using Barberos.Infrastructure;
using Barberos.Infrastructure.Auth;
using FluentValidation;
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

// CORS для React SPA
const string CorsPolicy = "spa";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

// Инфраструктура (EF Core + Postgres + auth-сервисы)
builder.Services.AddInfrastructure(builder.Configuration);

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

// Бутстрап первого админа (если админов нет и заданы Bootstrap:Admin:*).
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<AdminBootstrapper>();
    await bootstrapper.EnsureAdminAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => "Barberos API is running");

app.Run();
