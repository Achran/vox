namespace Vox.Application.Abstractions;

public interface ILiveKitService
{
    /// <summary>Generates a LiveKit access token for the given user and room.</summary>
    string GenerateToken(string userId, string displayName, string roomName);

    /// <summary>Gets the configured LiveKit server URL.</summary>
    string GetServerUrl();
}
