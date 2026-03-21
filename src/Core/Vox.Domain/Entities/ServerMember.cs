using Vox.Domain.Common;

namespace Vox.Domain.Entities;

public class ServerMember : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ServerId { get; private set; }
    public ServerRole Role { get; private set; }

    private ServerMember() { }

    public static ServerMember Create(Guid userId, Guid serverId, ServerRole role = ServerRole.Member)
    {
        return new ServerMember
        {
            UserId = userId,
            ServerId = serverId,
            Role = role
        };
    }

    public void UpdateRole(ServerRole role)
    {
        Role = role;
        SetUpdatedAt();
    }
}

public enum ServerRole
{
    Owner,
    Admin,
    Member
}
