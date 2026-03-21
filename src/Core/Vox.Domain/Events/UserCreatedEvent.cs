using Vox.Domain.Common;

namespace Vox.Domain.Events;

public sealed class UserCreatedEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string UserName { get; }
    public string Email { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public UserCreatedEvent(Guid userId, string userName, string email)
    {
        UserId = userId;
        UserName = userName;
        Email = email;
    }
}
