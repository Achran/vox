namespace Vox.Shared.UI.Services;

public interface IServerService
{
    Task<IReadOnlyList<ServerResponse>> GetUserServersAsync();
    Task<ServerResponse?> GetServerByIdAsync(Guid id);
    Task<ServerResponse?> CreateServerAsync(string name, string? description);
    Task<ServerResponse?> UpdateServerAsync(Guid id, string name, string? description);
    Task<bool> DeleteServerAsync(Guid id);
    string? ErrorMessage { get; }
}
