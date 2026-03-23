using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Vox.Application.Abstractions;
using Vox.Domain.Interfaces.Repositories;
using Vox.Infrastructure.Identity;
using Vox.Infrastructure.Persistence;
using Vox.Infrastructure.Persistence.Repositories;
using Vox.Infrastructure.Services;

namespace Vox.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public const string ExternalAuthCookieScheme = "ExternalAuth";

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<VoxDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<VoxDbContext>()
        .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSection);

        var externalSection = configuration.GetSection("ExternalAuth");
        services.Configure<ExternalAuthSettings>(externalSection);

        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

        if (secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret must be at least 32 characters long (256 bits) for sufficient cryptographic strength.");
        }

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Allow JWT in SignalR query string (for WebSocket connections)
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        })
        .AddCookie(ExternalAuthCookieScheme, options =>
        {
            options.Cookie.Name = "VoxExternalAuth";
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });

        AddExternalProviders(authBuilder, externalSection);

        services.AddSignalR();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();

        services.AddSingleton<IPresenceService, PresenceService>();
        services.AddHostedService<PresenceHeartbeatService>();

        return services;
    }

    private static void AddExternalProviders(
        AuthenticationBuilder authBuilder,
        IConfigurationSection externalSection)
    {
        var google = externalSection.GetSection("Google");
        if (google.Exists() && !string.IsNullOrEmpty(google["ClientId"]))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = google["ClientId"]!;
                options.ClientSecret = google["ClientSecret"]!;
                options.SignInScheme = ExternalAuthCookieScheme;
            });
        }

        var microsoft = externalSection.GetSection("Microsoft");
        if (microsoft.Exists() && !string.IsNullOrEmpty(microsoft["ClientId"]))
        {
            authBuilder.AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoft["ClientId"]!;
                options.ClientSecret = microsoft["ClientSecret"]!;
                options.SignInScheme = ExternalAuthCookieScheme;
            });
        }

        var facebook = externalSection.GetSection("Facebook");
        if (facebook.Exists() && !string.IsNullOrEmpty(facebook["ClientId"]))
        {
            authBuilder.AddFacebook(options =>
            {
                options.AppId = facebook["ClientId"]!;
                options.AppSecret = facebook["ClientSecret"]!;
                options.SignInScheme = ExternalAuthCookieScheme;
            });
        }

        var twitter = externalSection.GetSection("Twitter");
        if (twitter.Exists() && !string.IsNullOrEmpty(twitter["ClientId"]))
        {
            authBuilder.AddTwitter(options =>
            {
                options.ConsumerKey = twitter["ClientId"]!;
                options.ConsumerSecret = twitter["ClientSecret"]!;
                options.SignInScheme = ExternalAuthCookieScheme;
            });
        }

        var github = externalSection.GetSection("GitHub");
        if (github.Exists() && !string.IsNullOrEmpty(github["ClientId"]))
        {
            authBuilder.AddGitHub(options =>
            {
                options.ClientId = github["ClientId"]!;
                options.ClientSecret = github["ClientSecret"]!;
                options.SignInScheme = ExternalAuthCookieScheme;
            });
        }

        var steam = externalSection.GetSection("Steam");
        if (steam.Exists() && !string.IsNullOrEmpty(steam["ApplicationKey"]))
        {
            authBuilder.AddSteam(options =>
            {
                options.ApplicationKey = steam["ApplicationKey"]!;
                options.SignInScheme = ExternalAuthCookieScheme;
            });
        }
    }
}
