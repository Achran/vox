namespace Vox.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
