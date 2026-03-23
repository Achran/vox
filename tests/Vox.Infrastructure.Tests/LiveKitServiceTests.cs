using System.IdentityModel.Tokens.Jwt;
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Vox.Infrastructure.Services;

namespace Vox.Infrastructure.Tests;

public class LiveKitServiceTests
{
    private const string TestApiKey = "test-api-key";
    private const string TestApiSecret = "test-api-secret-at-least-32-characters-long!";
    private const string TestUrl = "ws://localhost:7880";

    private static LiveKitService CreateService(HttpMessageHandler? handler = null)
    {
        var settings = Options.Create(new LiveKitSettings
        {
            ApiKey = TestApiKey,
            ApiSecret = TestApiSecret,
            Url = TestUrl
        });

        var httpClient = handler is not null
            ? new HttpClient(handler)
            : new HttpClient(CreateMockHandler().Object);

        return new LiveKitService(settings, httpClient);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK, string content = "{}")
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        return handler;
    }

    // -------------------------------------------------------------------------
    // GenerateToken
    // -------------------------------------------------------------------------

    [Fact]
    public void GenerateToken_ReturnsValidJwt()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "Test User", "voice-room");

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "Test User", "voice-room");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be(TestApiKey);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "user-1");
        jwt.Claims.Should().Contain(c => c.Type == "name" && c.Value == "Test User");
        jwt.Claims.Should().Contain(c => c.Type == "jti");
    }

    [Fact]
    public void GenerateToken_ContainsVideoGrant()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "Test User", "voice-room");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Payload.Should().ContainKey("video");
        var videoGrant = jwt.Payload["video"] as System.Text.Json.JsonElement?;
        videoGrant.Should().NotBeNull();
        videoGrant!.Value.GetProperty("roomJoin").GetBoolean().Should().BeTrue();
        videoGrant.Value.GetProperty("room").GetString().Should().Be("voice-room");
    }

    [Fact]
    public void GenerateToken_SetsExpiry()
    {
        var service = CreateService();

        var token = service.GenerateToken("user-1", "Test User", "voice-room");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(6), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        var settings = Options.Create(new LiveKitSettings { ApiKey = "", ApiSecret = TestApiSecret, Url = TestUrl });
        var service = new LiveKitService(settings, new HttpClient());

        var act = () => service.GenerateToken("user-1", "Test User", "voice-room");

        act.Should().Throw<InvalidOperationException>().WithMessage("*ApiKey*");
    }

    [Fact]
    public void GenerateToken_WithMissingApiSecret_ThrowsInvalidOperationException()
    {
        var settings = Options.Create(new LiveKitSettings { ApiKey = TestApiKey, ApiSecret = "", Url = TestUrl });
        var service = new LiveKitService(settings, new HttpClient());

        var act = () => service.GenerateToken("user-1", "Test User", "voice-room");

        act.Should().Throw<InvalidOperationException>().WithMessage("*ApiSecret*");
    }

    // -------------------------------------------------------------------------
    // GetServerUrl
    // -------------------------------------------------------------------------

    [Fact]
    public void GetServerUrl_ReturnsConfiguredUrl()
    {
        var service = CreateService();

        service.GetServerUrl().Should().Be(TestUrl);
    }

    // -------------------------------------------------------------------------
    // GenerateServiceToken (internal)
    // -------------------------------------------------------------------------

    [Fact]
    public void GenerateServiceToken_WithRoomCreate_ContainsGrant()
    {
        var service = CreateService();

        var token = service.GenerateServiceToken(roomCreate: true);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Payload.Should().ContainKey("video");
        var videoGrant = jwt.Payload["video"] as System.Text.Json.JsonElement?;
        videoGrant.Should().NotBeNull();
        videoGrant!.Value.GetProperty("roomCreate").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void GenerateServiceToken_HasShortExpiry()
    {
        var service = CreateService();

        var token = service.GenerateServiceToken(roomCreate: true);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromMinutes(1));
    }

    // -------------------------------------------------------------------------
    // CreateRoomAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateRoomAsync_SendsCorrectRequest()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"name\":\"voice-room\"}")
            });

        var service = CreateService(handler.Object);

        await service.CreateRoomAsync("voice-room");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Contain("/twirp/livekit.RoomService/CreateRoom");
        capturedRequest.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
    }

    [Fact]
    public async Task CreateRoomAsync_ReturnsRoomName()
    {
        var service = CreateService();

        var result = await service.CreateRoomAsync("voice-room");

        result.Should().Be("voice-room");
    }

    [Fact]
    public async Task CreateRoomAsync_UsesHttpUrlFromWsUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

        var service = CreateService(handler.Object);

        await service.CreateRoomAsync("voice-room");

        capturedRequest!.RequestUri!.Scheme.Should().Be("http");
    }

    // -------------------------------------------------------------------------
    // DeleteRoomAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteRoomAsync_SendsCorrectRequest()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

        var service = CreateService(handler.Object);

        await service.DeleteRoomAsync("voice-room");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Contain("/twirp/livekit.RoomService/DeleteRoom");
        capturedRequest.Headers.Authorization.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteRoomAsync_WhenServerReturnsError_ThrowsException()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "error");
        var service = CreateService(handler.Object);

        var act = () => service.DeleteRoomAsync("voice-room");

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
