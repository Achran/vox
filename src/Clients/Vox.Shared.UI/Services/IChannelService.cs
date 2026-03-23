namespace Vox.Shared.UI.Services;

public interface IChannelService
{
    Task<IReadOnlyList<ChannelResponse>> GetServerChannelsAsync(Guid serverId);
    Task<ChannelResponse?> GetChannelByIdAsync(Guid id);
    Task<ChannelResponse?> CreateChannelAsync(Guid serverId, string name, string type);
    Task<ChannelResponse?> UpdateChannelAsync(Guid id, string name);
    Task<bool> DeleteChannelAsync(Guid id);
    string? ErrorMessage { get; }
}
