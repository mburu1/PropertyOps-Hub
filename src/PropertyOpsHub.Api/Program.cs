using Microsoft.AspNetCore.Http;
using PropertyOpsHub.Api.ErrorHandling;
using PropertyOpsHub.Api.Extensions;
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
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddApiRateLimiting();
builder.Services.AddHealthChecks();

// OpenAPI is generated at runtime in Development only; see Program below.
builder.Services.AddOpenApi();

var app = builder.Build();

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

// Liveness only: no database probe, so a saturated database does not restart healthy pods.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

/// <summary>Entry point marker so integration tests can host the real pipeline.</summary>
public partial class Program;
