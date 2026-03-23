namespace Vox.Application.Abstractions;

public interface IVoiceSessionService
{
    /// <summary>Adds a user to a voice channel. Returns true if user was not already in the channel.</summary>
    bool JoinChannel(string channelId, string userId, string connectionId);

    /// <summary>Removes a user from a voice channel. Returns true if user has no remaining connections in the channel.</summary>
    bool LeaveChannel(string channelId, string userId, string connectionId);

    /// <summary>Removes all voice sessions for a given connection. Returns the channel IDs that were left.</summary>
    IReadOnlyList<string> RemoveConnection(string connectionId);

    /// <summary>Gets the user IDs of all participants in a voice channel.</summary>
    IReadOnlyList<string> GetParticipants(string channelId);

    /// <summary>Returns true if the user is currently in the given voice channel.</summary>
    bool IsUserInVoiceChannel(string channelId, string userId);
}
