using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using FraudEngine.Api.Middleware;
using FraudEngine.Api.Validators;
using FraudEngine.Core.Data;
using FraudEngine.Core.Repositories;
using FraudEngine.Core.Rules;

var builder = WebApplication.CreateBuilder(args);

// Connection string is sourced from configuration (appsettings.json / appsettings.{Environment}.json),
// and can be overridden in any environment via the standard ASP.NET Core env var convention:
//   ConnectionStrings__DefaultConnection=Host=...;Port=...;Database=...;Username=...;Password=...
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No connection string configured. Set 'ConnectionStrings:DefaultConnection' in appsettings.json " +
        "or via the ConnectionStrings__DefaultConnection environment variable.");
}

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FluentValidation - validators run automatically as part of MVC model binding
// (in addition to any DataAnnotations on the request DTOs), producing a standard
// ValidationProblemDetails (400) response for invalid input.
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<TransactionRequestValidator>();

// EF Core with Npgsql. EnableRetryOnFailure adds resilience against transient
// connectivity issues (e.g. the database container still starting up, brief
// network blips), retrying with backoff instead of crashing the app.
builder.Services.AddDbContext<FraudDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

// Repository
builder.Services.AddScoped<IRepository, EfRepository>();

// Rule thresholds are configuration-driven - see the "RuleOptions" section of
// appsettings.json / appsettings.{Environment}.json.
builder.Services.Configure<RuleOptions>(builder.Configuration.GetSection("RuleOptions"));

// Rules registration
builder.Services.AddSingleton<IFraudRule, HighAmountRule>();
builder.Services.AddScoped<IFraudRule, RapidTransactionsRule>();
builder.Services.AddScoped<IFraudRule, VelocityAmountRule>();
builder.Services.AddScoped<IFraudRule, UnusualCountryRule>();
builder.Services.AddScoped<IFraudRule, NightTimeWithdrawalRule>();
builder.Services.AddScoped<IFraudRule, MerchantCategoryRule>();
builder.Services.AddScoped<IFraudRule, AccountAgeRule>();
builder.Services.AddScoped<RulesEngine>();

var app = builder.Build();

// Apply pending EF Core migrations at startup. This creates the database schema
// (and keeps it up to date) without requiring a separate manual migration step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FraudDbContext>();

    // Migrate() is a single operation and isn't automatically retried by the
    // EnableRetryOnFailure execution strategy configured above, so it's wrapped
    // explicitly here - the documented pattern for resilient startup migrations.
    var strategy = db.Database.CreateExecutionStrategy();
    await strategy.ExecuteAsync(() => db.Database.MigrateAsync());

    // Seed a handful of demo transactions (and the alerts they naturally produce)
    // so the API is browsable immediately in local/dev environments, without
    // requiring a manual POST first. Never runs in Production, and is a no-op if
    // the database already has data - see DbSeeder for details.
    if (app.Environment.IsDevelopment())
    {
        var repository = scope.ServiceProvider.GetRequiredService<IRepository>();
        var rulesEngine = scope.ServiceProvider.GetRequiredService<RulesEngine>();
        await DbSeeder.SeedAsync(db, repository, rulesEngine);
    }
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Redirect the bare root URL to Swagger so `docker compose up` -> browsing to
    // http://localhost:8080/ lands somewhere useful instead of a 404.
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

// Lightweight liveness endpoint - useful for container/orchestrator health checks
// in any environment (unlike Swagger, this is always available).
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Registered before UseRouting so it wraps the entire pipeline downstream,
// converting any unhandled exception into a standard problem+json response.
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
