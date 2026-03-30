using Auth.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserWorkspace> UserWorkspaces => Set<UserWorkspace>();
    public DbSet<UserWorkspaceRole> UserWorkspaceRoles => Set<UserWorkspaceRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();
    public DbSet<PasswordChangeChallenge> PasswordChangeChallenges => Set<PasswordChangeChallenge>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<IdentitySource> IdentitySources => Set<IdentitySource>();
    public DbSet<IdentitySourceOidcConfig> IdentitySourceOidcConfigs => Set<IdentitySourceOidcConfig>();
    public DbSet<IdentitySourceLdapConfig> IdentitySourceLdapConfigs => Set<IdentitySourceLdapConfig>();
    public DbSet<IdentitySourceLink> IdentitySourceLinks => Set<IdentitySourceLink>();
    public DbSet<Domain.Application> Applications => Set<Domain.Application>();
    public DbSet<ServiceAccount> ServiceAccounts => Set<ServiceAccount>();
    public DbSet<ServiceAccountWorkspace> ServiceAccountWorkspaces => Set<ServiceAccountWorkspace>();
    public DbSet<ServiceAccountWorkspaceRole> ServiceAccountWorkspaceRoles => Set<ServiceAccountWorkspaceRole>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
        modelBuilder.UseOpenIddict();

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    private void SetTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
