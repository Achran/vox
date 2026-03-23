using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Vox.Infrastructure.Persistence;

namespace Vox.Api.Tests.Fixtures;

/// <summary>
/// A custom WebApplicationFactory that replaces the real PostgreSQL database with an
/// in-memory database and provides minimal test configuration for JWT and other settings.
/// </summary>
public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"VoxTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide test configuration BEFORE services are registered so that
        // InfrastructureServiceExtensions picks up the test JWT settings.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;",
                ["JwtSettings:Secret"] = TestJwtHelper.Secret,
                ["JwtSettings:Issuer"] = TestJwtHelper.Issuer,
                ["JwtSettings:Audience"] = TestJwtHelper.Audience,
                ["JwtSettings:ExpiryMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpiryDays"] = "7",
            });
        });

        // ConfigureTestServices runs AFTER all app service registrations.
        builder.ConfigureTestServices(services =>
        {
            // ---------------------------------------------------------------
            // Fix JWT bearer signing key.
            //
            // InfrastructureServiceExtensions eagerly captures the secret string
            // from IConfiguration into the AddJwtBearer closure *before* the test
            // InMemoryCollection overrides are applied (the overrides arrive after
            // WebApplication.CreateBuilder has already loaded appsettings.json).
            // The issuer/audience are read lazily inside the closure and therefore
            // pick up the test values; the pre-captured secret does not.
            // We correct this by registering an additional IConfigureOptions that
            // overwrites the IssuerSigningKey with the test secret.
            // ---------------------------------------------------------------
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtHelper.Secret));
            });

            // ---------------------------------------------------------------
            // Replace the PostgreSQL DbContext with an in-memory one.
            // ---------------------------------------------------------------

            // Remove the Npgsql options configuration action stored by AddInfrastructureServices.
            // IDbContextOptionsConfiguration<T> holds the UseNpgsql(...) lambda; removing it
            // prevents EF Core from seeing two competing database providers.
            var optionsConfigs = services
                .Where(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<VoxDbContext>))
                .ToList();
            foreach (var d in optionsConfigs)
                services.Remove(d);

            // Remove the cached DbContextOptions descriptors so they are rebuilt from scratch.
            var cachedOptions = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<VoxDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var d in cachedOptions)
                services.Remove(d);

            // Add a fresh InMemory DbContext registration.
            // Suppress TransactionIgnoredWarning because IdentityService uses transactions
            // which InMemory silently ignores.
            services.AddDbContext<VoxDbContext>(options =>
                options
                    .UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            // Ensure the schema is created before any test runs.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VoxDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}
