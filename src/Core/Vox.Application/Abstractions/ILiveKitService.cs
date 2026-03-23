namespace Vox.Application.Abstractions;

public interface ILiveKitService
{
    /// <summary>Generates a LiveKit access token for the given user and room.</summary>
    string GenerateToken(string userId, string displayName, string roomName);

    /// <summary>Gets the configured LiveKit server URL.</summary>
    string GetServerUrl();

    /// <summary>Creates a new room on the LiveKit server. Returns the room name.</summary>
    Task<string> CreateRoomAsync(string roomName, CancellationToken cancellationToken = default);

    /// <summary>Deletes a room on the LiveKit server.</summary>
    Task DeleteRoomAsync(string roomName, CancellationToken cancellationToken = default);
}
