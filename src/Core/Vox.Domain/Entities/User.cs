using Vox.Domain.Common;
using Vox.Domain.Events;

namespace Vox.Domain.Entities;

public class User : BaseEntity
{
    public string UserName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Offline;

    private User() { }

    public static User Create(string userName, string email, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var user = new User
        {
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            Status = UserStatus.Offline
        };

        user.AddDomainEvent(new UserCreatedEvent(user.Id, userName, email));
        return user;
    }

    public void UpdateStatus(UserStatus status)
    {
        Status = status;
        SetUpdatedAt();
    }

    public void UpdateProfile(string displayName, string? avatarUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        SetUpdatedAt();
    }
}

public enum UserStatus
{
    Online,
    Away,
    DoNotDisturb,
    Offline
}
