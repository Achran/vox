namespace Vox.Shared.UI.Services;

public interface IMessageService
{
    Task<IReadOnlyList<MessageResponse>> GetChannelMessagesAsync(Guid channelId, int pageSize = 50, DateTime? before = null);
    string? ErrorMessage { get; }
}
