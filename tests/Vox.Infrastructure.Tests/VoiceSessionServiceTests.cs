using FluentAssertions;
using Vox.Infrastructure.Services;

namespace Vox.Infrastructure.Tests;

public class VoiceSessionServiceTests
{
    private readonly VoiceSessionService _service = new();

    // -------------------------------------------------------------------------
    // JoinChannel
    // -------------------------------------------------------------------------

    [Fact]
    public void JoinChannel_FirstConnection_ReturnsTrue()
    {
        var result = _service.JoinChannel("channel-1", "user-1", "conn-1");

        result.Should().BeTrue();
    }

    [Fact]
    public void JoinChannel_SecondConnectionSameUser_ReturnsFalse()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");

        var result = _service.JoinChannel("channel-1", "user-1", "conn-2");

        result.Should().BeFalse();
    }

    [Fact]
    public void JoinChannel_DifferentUsers_BothReturnTrue()
    {
        var result1 = _service.JoinChannel("channel-1", "user-1", "conn-1");
        var result2 = _service.JoinChannel("channel-1", "user-2", "conn-2");

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // LeaveChannel
    // -------------------------------------------------------------------------

    [Fact]
    public void LeaveChannel_LastConnection_ReturnsTrue()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");

        var result = _service.LeaveChannel("channel-1", "user-1", "conn-1");

        result.Should().BeTrue();
    }

    [Fact]
    public void LeaveChannel_NotLastConnection_ReturnsFalse()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.JoinChannel("channel-1", "user-1", "conn-2");

        var result = _service.LeaveChannel("channel-1", "user-1", "conn-1");

        result.Should().BeFalse();
    }

    [Fact]
    public void LeaveChannel_NonExistentChannel_ReturnsFalse()
    {
        var result = _service.LeaveChannel("channel-1", "user-1", "conn-1");

        result.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // GetParticipants
    // -------------------------------------------------------------------------

    [Fact]
    public void GetParticipants_ReturnsCurrentUsers()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.JoinChannel("channel-1", "user-2", "conn-2");

        var participants = _service.GetParticipants("channel-1");

        participants.Should().BeEquivalentTo(new[] { "user-1", "user-2" });
    }

    [Fact]
    public void GetParticipants_EmptyChannel_ReturnsEmpty()
    {
        var participants = _service.GetParticipants("channel-1");

        participants.Should().BeEmpty();
    }

    [Fact]
    public void GetParticipants_AfterUserLeaves_ExcludesUser()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.JoinChannel("channel-1", "user-2", "conn-2");
        _service.LeaveChannel("channel-1", "user-1", "conn-1");

        var participants = _service.GetParticipants("channel-1");

        participants.Should().BeEquivalentTo(new[] { "user-2" });
    }

    // -------------------------------------------------------------------------
    // IsUserInVoiceChannel
    // -------------------------------------------------------------------------

    [Fact]
    public void IsUserInVoiceChannel_WhenJoined_ReturnsTrue()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");

        _service.IsUserInVoiceChannel("channel-1", "user-1").Should().BeTrue();
    }

    [Fact]
    public void IsUserInVoiceChannel_WhenNotJoined_ReturnsFalse()
    {
        _service.IsUserInVoiceChannel("channel-1", "user-1").Should().BeFalse();
    }

    [Fact]
    public void IsUserInVoiceChannel_AfterLeaving_ReturnsFalse()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.LeaveChannel("channel-1", "user-1", "conn-1");

        _service.IsUserInVoiceChannel("channel-1", "user-1").Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // RemoveConnection
    // -------------------------------------------------------------------------

    [Fact]
    public void RemoveConnection_ReturnsLeftChannels()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.JoinChannel("channel-2", "user-1", "conn-1");

        var leftChannels = _service.RemoveConnection("conn-1");

        leftChannels.Should().BeEquivalentTo(new[] { "channel-1", "channel-2" });
    }

    [Fact]
    public void RemoveConnection_RemovesUserFromChannels()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.RemoveConnection("conn-1");

        _service.IsUserInVoiceChannel("channel-1", "user-1").Should().BeFalse();
        _service.GetParticipants("channel-1").Should().BeEmpty();
    }

    [Fact]
    public void RemoveConnection_NonExistentConnection_ReturnsEmpty()
    {
        var leftChannels = _service.RemoveConnection("conn-nonexistent");

        leftChannels.Should().BeEmpty();
    }

    [Fact]
    public void RemoveConnection_OtherUserConnectionsRemain()
    {
        _service.JoinChannel("channel-1", "user-1", "conn-1");
        _service.JoinChannel("channel-1", "user-1", "conn-2");
        _service.RemoveConnection("conn-1");

        _service.IsUserInVoiceChannel("channel-1", "user-1").Should().BeTrue();
        _service.GetParticipants("channel-1").Should().Contain("user-1");
    }
}
