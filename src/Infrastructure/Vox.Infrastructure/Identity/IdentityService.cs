using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Vox.Application.Abstractions;
using Vox.Application.DTOs;
using Vox.Domain.Entities;
using Vox.Infrastructure.Persistence;

namespace Vox.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly VoxDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        VoxDbContext context,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<AuthTokensDto> RegisterAsync(
        string userName,
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var domainUser = User.Create(userName, email, displayName);

        var appUser = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            DisplayName = displayName,
            DomainUserId = domainUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            var errors = result.Errors.Select(e => e.Description).ToArray();
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        _context.DomainUsers.Add(domainUser);
        await _context.SaveChangesAsync(cancellationToken);

        var tokens = await GenerateAndStoreTokensAsync(appUser, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return BuildDto(appUser, tokens);
    }

    public async Task<AuthTokensDto> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(email);
        if (appUser is null || !await _userManager.CheckPasswordAsync(appUser, password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var tokens = await GenerateAndStoreTokensAsync(appUser, cancellationToken);
        return BuildDto(appUser, tokens);
    }

    public async Task<AuthTokensDto> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        stored.IsRevoked = true;

        var tokens = await GenerateAndStoreTokensAsync(stored.User, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return BuildDto(stored.User, tokens);
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (stored is not null && !stored.IsRevoked)
        {
            stored.IsRevoked = true;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AuthTokensDto> ExternalLoginAsync(
        string provider,
        string providerKey,
        string? email,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        // Try to find an existing user linked to this external login
        var appUser = await _userManager.FindByLoginAsync(provider, providerKey);

        if (appUser is not null)
        {
            var tokens = await GenerateAndStoreTokensAsync(appUser, cancellationToken);
            return BuildDto(appUser, tokens);
        }

        // No existing external login link — check if there's a user with matching email
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (email is not null)
        {
            appUser = await _userManager.FindByEmailAsync(email);
        }

        if (appUser is null)
        {
            // Create a brand new user
            var userName = GenerateUniqueUserName(provider, providerKey);
            var userEmail = email ?? $"{userName}@external.vox";
            var userDisplayName = displayName ?? userName;

            var domainUser = User.Create(userName, userEmail, userDisplayName);

            appUser = new ApplicationUser
            {
                UserName = userName,
                Email = userEmail,
                DisplayName = userDisplayName,
                DomainUserId = domainUser.Id,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = email is not null
            };

            var createResult = await _userManager.CreateAsync(appUser);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                var errors = createResult.Errors.Select(e => e.Description).ToArray();
                throw new InvalidOperationException(string.Join("; ", errors));
            }

            _context.DomainUsers.Add(domainUser);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Link the external login to the user
        var loginResult = await _userManager.AddLoginAsync(appUser,
            new UserLoginInfo(provider, providerKey, provider));
        if (!loginResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            var errors = loginResult.Errors.Select(e => e.Description).ToArray();
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        var authTokens = await GenerateAndStoreTokensAsync(appUser, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return BuildDto(appUser, authTokens);
    }

    public async Task<IReadOnlyList<LinkedAccountDto>> GetLinkedProvidersAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var logins = await _userManager.GetLoginsAsync(appUser);

        return logins
            .Select(l => new LinkedAccountDto(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
            .ToList();
    }

    public async Task LinkExternalProviderAsync(
        string userId,
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var result = await _userManager.AddLoginAsync(appUser,
            new UserLoginInfo(provider, providerKey, provider));

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            throw new InvalidOperationException(string.Join("; ", errors));
        }
    }

    public async Task UnlinkExternalProviderAsync(
        string userId,
        string provider,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var logins = await _userManager.GetLoginsAsync(appUser);
        var hasPassword = await _userManager.HasPasswordAsync(appUser);

        // Ensure at least one login method remains after removal
        var totalMethods = logins.Count + (hasPassword ? 1 : 0);
        if (totalMethods <= 1)
        {
            throw new InvalidOperationException("Cannot unlink the last login method.");
        }

        var login = logins.FirstOrDefault(l =>
            l.LoginProvider.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (login is null)
        {
            throw new InvalidOperationException($"Provider '{provider}' is not linked to this account.");
        }

        var result = await _userManager.RemoveLoginAsync(appUser, login.LoginProvider, login.ProviderKey);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            throw new InvalidOperationException(string.Join("; ", errors));
        }
    }

    private static string GenerateUniqueUserName(string provider, string providerKey)
    {
        // Produce a short deterministic username: provider_first8charsOfHash
        var hash = Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.UTF8.GetBytes($"{provider}:{providerKey}")));
        return $"{provider.ToLowerInvariant()}_{hash[..8]}";
    }

    private async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateAndStoreTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var (accessToken, expiresAt) = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return (accessToken, refreshToken, expiresAt);
    }

    private (string Token, DateTime ExpiresAt) GenerateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("display_name", user.DisplayName)
        };

        if (user.DomainUserId.HasValue)
        {
            claims.Add(new Claim("domain_user_id", user.DomainUserId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static AuthTokensDto BuildDto(
        ApplicationUser user,
        (string AccessToken, string RefreshToken, DateTime ExpiresAt) tokens)
        => new(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresAt,
            user.Id,
            user.Email!,
            user.UserName!,
            user.DisplayName);
}
