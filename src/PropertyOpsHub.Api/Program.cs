using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using PropertyOpsHub.Api.ErrorHandling;
using PropertyOpsHub.Api.Extensions;
using PropertyOpsHub.Api.Logging;
using PropertyOpsHub.Api.Security;
using PropertyOpsHub.Application;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Containers want one JSON object per line; a developer wants readable output.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

var connectionString = builder.Configuration.GetConnectionString("SqlServer");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:SqlServer is not configured. Set it in user secrets locally or in the " +
        "environment (ConnectionStrings__SqlServer) when deployed.");
}

builder.Services.AddControllers();

// One error format everywhere: controllers, domain exceptions and the rate limiter all emit
// RFC 9457 problem details.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

// Authentication is a capability, not a prerequisite for booting: an instance without a signing
// key starts, reports itself unready and refuses every protected endpoint. Crashing instead
// would only show a restart counter.
var authentication = AuthenticationState.From(builder.Configuration);
builder.Services.AddSingleton(authentication);

if (authentication.IsConfigured)
{
    builder.Services.AddJwtAuthentication(builder.Configuration);
}
else
{
    builder.Services
        .AddAuthentication(UnconfiguredAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, UnconfiguredAuthenticationHandler>(
            UnconfiguredAuthenticationHandler.SchemeName,
            configureOptions: null);
}

builder.Services.AddAuthorizationPolicies();
builder.Services.AddApiRateLimiting();
builder.Services.AddHealthChecks();

// OpenAPI is generated at runtime in Development only; see Program below.
builder.Services.AddOpenApi();

var app = builder.Build();

if (!authentication.IsConfigured)
{
    ApiLogMessages.AuthenticationNotConfigured(app.Logger, authentication.Problem);
}

app.UseExceptionHandler();
app.UseProblemDetailsForStatusCodes();

if (app.Environment.IsDevelopment())
{
    // Documented and explorable only locally: an API surface map is free reconnaissance.
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Liveness: is the process up? No database probe, so a saturated database does not restart
// healthy pods.
app.MapHealthChecks("/health").AllowAnonymous();

// Readiness: can this instance actually serve traffic? Today that means "has credentials".
app.MapGet(
        "/ready",
        (AuthenticationState state) => state.IsConfigured
            ? Results.Ok(new { status = "ready" })
            : Results.Json(
                new { status = "not_ready", problems = new[] { state.Problem } },
                statusCode: StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous()
    .WithName("Readiness");

app.Run();

/// <summary>Entry point marker so integration tests can host the real pipeline.</summary>
public partial class Program;
