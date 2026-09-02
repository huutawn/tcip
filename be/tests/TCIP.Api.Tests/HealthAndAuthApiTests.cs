using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TCIP.Business.Modules.Identity.Application.Contracts;
using Xunit;

namespace TCIP.Api.Tests;

public sealed class HealthAndAuthApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("ok", body.Status);
    }

    [Fact]
    public async Task AuthFlow_RegisterLoginRefresh_Succeeds()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "Password123!";
        var displayName = "Test User";

        // Register
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, displayName));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var regBody = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>(JsonOptions);
        Assert.NotNull(regBody);
        Assert.Equal(email, regBody.Email);

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(loginBody);
        Assert.NotEmpty(loginBody.AccessToken);
        Assert.NotEmpty(loginBody.RefreshToken);

        // Refresh
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(loginBody.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(refreshBody);
        Assert.NotEmpty(refreshBody.AccessToken);
        Assert.NotEmpty(refreshBody.RefreshToken);
    }

    private sealed record HealthResponse(string Status);
}
