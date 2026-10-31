using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace PropertyOpsHub.Api.IntegrationTests.Endpoints;

/// <summary>
/// Hosts the real pipeline and asserts the behaviour the CD smoke step used to check:
/// liveness, readiness, and a closed door for unauthenticated callers.
/// </summary>
/// <remarks>
/// The database is never touched: every request here is rejected before a handler runs, or is
/// answered by /health and /ready. That keeps the suite fast and dependency free.
/// </remarks>
public class ApiPipelineTests
{
    private const string TestIssuer = "https://tests.local";
    private const string TestAudience = "propertyopshub-tests";
    private const string Scheme = "Bearer";

    /// <summary>Generated per run: a value committed to source control is a value someone reuses.</summary>
    private static readonly string TestSigningKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static WebApplicationFactory<Program> CreateFactory(
        string? environment = null,
        string? signingKey = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment ?? Environments.Production);
            builder.UseSetting("ConnectionStrings:SqlServer", "Server=unused;Database=unused;");
            builder.UseSetting("Jwt:Issuer", TestIssuer);
            builder.UseSetting("Jwt:Audience", TestAudience);
            builder.UseSetting("Jwt:SigningKey", signingKey ?? TestSigningKey);
        });
    }

    /// <summary>An instance with no usable key: readiness must say so and requests must fail.</summary>
    private static WebApplicationFactory<Program> CreateUnconfiguredFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.UseSetting("ConnectionStrings:SqlServer", "Server=unused;Database=unused;");
            builder.UseSetting("Jwt:Issuer", TestIssuer);
            builder.UseSetting("Jwt:Audience", TestAudience);
            builder.UseSetting("Jwt:SigningKey", string.Empty);
        });
    }

    private static string CreateCredential(string role, string? key = null)
    {
        var signingKey = key ?? TestSigningKey;

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims:
            [
                new Claim("sub", Guid.CreateVersion7().ToString()),
                new Claim("role", role),
                new Claim("email", "tester@demo.local")
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    [Fact]
    public async Task Health_answers_even_when_authentication_is_not_configured()
    {
        using var factory = CreateUnconfiguredFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_ready_when_configuration_is_complete()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_the_missing_setting_instead_of_crashing()
    {
        using var factory = CreateUnconfiguredFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("not_ready", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Jwt:SigningKey", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_protected_endpoint_rejects_anonymous_callers_with_problem_details()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/properties/{Guid.CreateVersion7()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("auth.unauthenticated", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unconfigured_instance_rejects_every_protected_endpoint()
    {
        using var factory = CreateUnconfiguredFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/properties/{Guid.CreateVersion7()}");

        // Fail closed: nobody can be authenticated, so nothing is served.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_valid_credential_with_the_wrong_role_is_forbidden()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/properties");
        request.Headers.TryAddWithoutValidation("Authorization", $"{Scheme} {CreateCredential("StoreController")}");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                groupId = Guid.CreateVersion7(),
                name = "Nairobi CBD",
                code = "NBO-CBD",
                timeZoneId = "Africa/Nairobi"
            }),
            Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("auth.forbidden", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_credential_signed_with_another_key_is_rejected()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/properties/{Guid.CreateVersion7()}");
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"{Scheme} {CreateCredential("GroupFinance", key: Convert.ToHexString(RandomNumberGenerator.GetBytes(32)))}");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_api_map_is_served_in_development_only()
    {
        using (var development = CreateFactory(Environments.Development))
        {
            using var client = development.CreateClient();
            var inDevelopment = await client.GetAsync("/openapi/v1.json");

            Assert.Equal(HttpStatusCode.OK, inDevelopment.StatusCode);
        }

        using var production = CreateFactory();
        using var productionClient = production.CreateClient();
        var inProduction = await productionClient.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, inProduction.StatusCode);
    }

    [Fact]
    public async Task An_unknown_route_returns_problem_details()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/nope");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("endpoint.not_found", body, StringComparison.Ordinal);
    }
}
