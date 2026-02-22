using Auth.Domain;
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
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TwoFactorChallenge> TwoFactorChallenges => Set<TwoFactorChallenge>();
    public DbSet<PasswordChangeChallenge> PasswordChangeChallenges => Set<PasswordChangeChallenge>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
