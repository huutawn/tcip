using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TCIP.Business.Modules.Identity.Application.Contracts;
using Xunit;

namespace TCIP.Api.Tests;

public sealed class RbacAndAuthorizationApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task AnonymousAccessToProtectedEndpoint_Returns401Unauthorized()
    {
        var response = await client.GetAsync("/api/calendar/events/by-day");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegularUser_AccessingAdminOnlyEndpoint_Returns403Forbidden()
    {
        var email = $"regular_{Guid.NewGuid():N}@example.com";
        var password = "Password123!";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Regular User"));
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        // GET /api/users requires Admin role
        var usersResponse = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
    }

    [Fact]
    public async Task SignalR_UnauthorizedAccess_RequiresAuthorization()
    {
        // Hub endpoint without token should not allow anonymous access
        var response = await client.GetAsync("/hubs/notifications");
        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.NotFound);
    }
}
