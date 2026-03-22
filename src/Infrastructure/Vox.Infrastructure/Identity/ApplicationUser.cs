using Microsoft.AspNetCore.Identity;

namespace Vox.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? DomainUserId { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
