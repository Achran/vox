using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Vox.Infrastructure.Hubs;

namespace Vox.Infrastructure.Tests;

public class ChatRateLimitFilterTests
{
    private readonly ChatRateLimitFilter _filter = new();

    private static HubInvocationContext CreateInvocationContext(string methodName, string? userIdentifier, string connectionId)
    {
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.UserIdentifier).Returns(userIdentifier);
        contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        var serviceProviderMock = new Mock<IServiceProvider>();
        var hubMock = new Mock<Hub>();

        return new HubInvocationContext(
            contextMock.Object,
            serviceProviderMock.Object,
            hubMock.Object,
            typeof(ChatHub).GetMethod(methodName)!,
            new List<object?> { Guid.NewGuid().ToString(), "Hello!" });
    }

    [Fact]
    public async Task InvokeMethodAsync_SendMessage_AllowsWithinLimit()
    {
        // Arrange – use a unique user for this test
        var userId = Guid.NewGuid().ToString();
        var context = CreateInvocationContext(nameof(ChatHub.SendMessage), userId, Guid.NewGuid().ToString());
        var nextCalled = false;

        // Act
        await _filter.InvokeMethodAsync(context, _ =>
        {
            nextCalled = true;
            return new ValueTask<object?>((object?)null);
        });

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeMethodAsync_SendMessage_ThrowsWhenRateLimitExceeded()
    {
        // Arrange – use a unique user for this test
        var userId = Guid.NewGuid().ToString();

        // Send 10 messages (the limit)
        for (var i = 0; i < 10; i++)
        {
            var ctx = CreateInvocationContext(nameof(ChatHub.SendMessage), userId, Guid.NewGuid().ToString());
            await _filter.InvokeMethodAsync(ctx, _ => new ValueTask<object?>((object?)null));
        }

        // Act – the 11th should fail
        var context = CreateInvocationContext(nameof(ChatHub.SendMessage), userId, Guid.NewGuid().ToString());
        var act = () => _filter.InvokeMethodAsync(context, _ => new ValueTask<object?>((object?)null)).AsTask();

        // Assert
        await act.Should().ThrowAsync<HubException>()
            .WithMessage("Rate limit exceeded*");
    }

    [Fact]
    public async Task InvokeMethodAsync_NonSendMessageMethod_BypassesRateLimit()
    {
        // Arrange – use a unique user and a non-SendMessage method
        var userId = Guid.NewGuid().ToString();
        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.UserIdentifier).Returns(userId);
        contextMock.Setup(c => c.ConnectionId).Returns(Guid.NewGuid().ToString());

        var serviceProviderMock = new Mock<IServiceProvider>();
        var hubMock = new Mock<Hub>();

        var context = new HubInvocationContext(
            contextMock.Object,
            serviceProviderMock.Object,
            hubMock.Object,
            typeof(ChatHub).GetMethod(nameof(ChatHub.StartTyping))!,
            new List<object?> { Guid.NewGuid().ToString() });

        var nextCalled = false;

        // Act
        await _filter.InvokeMethodAsync(context, _ =>
        {
            nextCalled = true;
            return new ValueTask<object?>((object?)null);
        });

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeMethodAsync_DifferentUsers_HaveIndependentLimits()
    {
        // Arrange – two different users
        var user1 = Guid.NewGuid().ToString();
        var user2 = Guid.NewGuid().ToString();

        // Exhaust user1's limit
        for (var i = 0; i < 10; i++)
        {
            var ctx = CreateInvocationContext(nameof(ChatHub.SendMessage), user1, Guid.NewGuid().ToString());
            await _filter.InvokeMethodAsync(ctx, _ => new ValueTask<object?>((object?)null));
        }

        // Act – user2 should still be able to send
        var context = CreateInvocationContext(nameof(ChatHub.SendMessage), user2, Guid.NewGuid().ToString());
        var nextCalled = false;
        await _filter.InvokeMethodAsync(context, _ =>
        {
            nextCalled = true;
            return new ValueTask<object?>((object?)null);
        });

        // Assert
        nextCalled.Should().BeTrue();
    }
}
