using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class User : Entity
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Offline;

    private readonly List<ServerMembership> _serverMemberships = [];
    public IReadOnlyCollection<ServerMembership> ServerMemberships => _serverMemberships.AsReadOnly();

    private User() { }

    public static User Create(string username, string email, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var user = new User
        {
            Username = username,
            Email = email,
            DisplayName = displayName
        };

        return user;
    }

    public void UpdateStatus(UserStatus status) => Status = status;
    public void UpdateAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;
}

public enum UserStatus
{
    Online,
    Idle,
    DoNotDisturb,
    Offline
}

public class ServerMembership : Entity
{
    public Guid UserId { get; private set; }
    public Guid ServerId { get; private set; }
    public string Role { get; private set; } = "Member";

    private ServerMembership() { }

    public static ServerMembership Create(Guid userId, Guid serverId, string role = "Member")
        => new() { UserId = userId, ServerId = serverId, Role = role };
}
