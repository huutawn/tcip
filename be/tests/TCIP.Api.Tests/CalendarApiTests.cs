using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TCIP.Business.Modules.Calendar.Application.Contracts;
using TCIP.Business.Modules.Identity.Application.Contracts;
using Xunit;

namespace TCIP.Api.Tests;

public sealed class CalendarApiTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private readonly HttpClient client = factory.CreateClient();

    private async Task<string> AuthenticateAsync()
    {
        var email = $"cal_user_{Guid.NewGuid():N}@example.com";
        var password = "Password123!";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, "Calendar User"));
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await loginRes.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return body!.AccessToken;
    }

    [Fact]
    public async Task CreateEvent_ReturnsCreatedAndETag()
    {
        var token = await AuthenticateAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startAt = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        var req = new CreateEventRequest
        {
            StartAt = startAt,
            Translations = [new EventTranslationRequest("en", "Team Sync", "Weekly sync")]
        };

        var response = await client.PostAsJsonAsync("/api/calendar/events", req);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.Equal("\"1\"", response.Headers.ETag.Tag);

        var body = await response.Content.ReadFromJsonAsync<CalendarEventDetailResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(1L, body.Version);
    }

    [Fact]
    public async Task UpdateEvent_MissingIfMatch_Returns428PreconditionRequired()
    {
        var token = await AuthenticateAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startAt = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        var createRes = await client.PostAsJsonAsync("/api/calendar/events", new CreateEventRequest
        {
            StartAt = startAt,
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        });
        var created = await createRes.Content.ReadFromJsonAsync<CalendarEventDetailResponse>(JsonOptions);

        var updateReq = new UpdateEventRequest
        {
            StartAt = startAt.AddHours(1),
            Translations = [new EventTranslationRequest("en", "Updated Meeting", null)]
        };

        var updateRes = await client.PutAsJsonAsync($"/api/calendar/events/{created!.Id}", updateReq);
        Assert.Equal(HttpStatusCode.PreconditionRequired, updateRes.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_StaleIfMatch_Returns412PreconditionFailed()
    {
        var token = await AuthenticateAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startAt = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        var createRes = await client.PostAsJsonAsync("/api/calendar/events", new CreateEventRequest
        {
            StartAt = startAt,
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        });
        var created = await createRes.Content.ReadFromJsonAsync<CalendarEventDetailResponse>(JsonOptions);

        var updateReq = new UpdateEventRequest
        {
            StartAt = startAt.AddHours(1),
            Translations = [new EventTranslationRequest("en", "Updated Meeting", null)]
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/calendar/events/{created!.Id}")
        {
            Content = JsonContent.Create(updateReq)
        };
        requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue("\"999\""));

        var updateRes = await client.SendAsync(requestMessage);
        Assert.Equal(HttpStatusCode.PreconditionFailed, updateRes.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ValidIfMatch_Returns200AndIncrementedETag()
    {
        var token = await AuthenticateAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var startAt = new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero);
        var createRes = await client.PostAsJsonAsync("/api/calendar/events", new CreateEventRequest
        {
            StartAt = startAt,
            Translations = [new EventTranslationRequest("en", "Meeting", null)]
        });
        var created = await createRes.Content.ReadFromJsonAsync<CalendarEventDetailResponse>(JsonOptions);

        var updateReq = new UpdateEventRequest
        {
            StartAt = startAt.AddHours(1),
            Translations = [new EventTranslationRequest("en", "Updated Meeting", null)]
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"/api/calendar/events/{created!.Id}")
        {
            Content = JsonContent.Create(updateReq)
        };
        requestMessage.Headers.IfMatch.Add(new EntityTagHeaderValue("\"1\""));

        var updateRes = await client.SendAsync(requestMessage);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        Assert.NotNull(updateRes.Headers.ETag);
        Assert.Equal("\"2\"", updateRes.Headers.ETag.Tag);
    }
}
