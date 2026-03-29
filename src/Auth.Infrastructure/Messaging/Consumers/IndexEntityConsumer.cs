using Auth.Application;
using Auth.Application.Messaging.Commands;
using Auth.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Messaging.Consumers;

internal sealed class IndexEntityConsumer(
    AuthDbContext dbContext,
    ISearchIndexService searchIndexService,
    ILogger<IndexEntityConsumer> logger) : IConsumer<IndexEntityRequested>
{
    public async Task Consume(ConsumeContext<IndexEntityRequested> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        switch (msg.EntityType)
        {
            case IndexEntityType.User:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.Users.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    u => new UserDto(u.Id, u.Username, u.FullName, u.Email, u.Phone,
                        u.IsActive, u.IsInternalAuthEnabled, u.MustChangePassword, u.TwoFactorEnabled, u.TwoFactorChannel,
                        u.Locale, u.EmailVerified, u.PhoneVerified, u.PasswordMaxAgeDays, u.PasswordChangedAt),
                    (dto, c) => searchIndexService.IndexUserAsync(dto, c),
                    c => searchIndexService.DeleteUserAsync(msg.EntityId, c));
                break;

            case IndexEntityType.Role:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.Roles.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    r => new RoleDto(r.Id, r.Name, r.Code, r.Description),
                    (dto, c) => searchIndexService.IndexRoleAsync(dto, c),
                    c => searchIndexService.DeleteRoleAsync(msg.EntityId, c));
                break;

            case IndexEntityType.Permission:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.Permissions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    p => new PermissionDto(p.Id, p.Domain, p.Bit, p.Code, p.Description, p.IsSystem),
                    (dto, c) => searchIndexService.IndexPermissionAsync(dto, c),
                    c => searchIndexService.DeletePermissionAsync(msg.EntityId, c));
                break;

            case IndexEntityType.Workspace:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.Workspaces.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    w => new WorkspaceDto(w.Id, w.Name, w.Code, w.Description, w.IsSystem),
                    (dto, c) => searchIndexService.IndexWorkspaceAsync(dto, c),
                    c => searchIndexService.DeleteWorkspaceAsync(msg.EntityId, c));
                break;

            case IndexEntityType.Application:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.Applications.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    a => new ApplicationDto(a.Id, a.Name, a.Description, a.ClientId, a.IsActive,
                        a.IsConfidential, a.LogoUrl, a.HomepageUrl,
                        a.RedirectUris, a.PostLogoutRedirectUris, a.AllowedOrigins, a.Scopes,
                        a.GrantTypes, a.Audiences, a.AccessTokenLifetimeMinutes, a.RefreshTokenLifetimeMinutes,
                        a.RequireEmailVerified, a.RequirePhoneVerified),
                    (dto, c) => searchIndexService.IndexApplicationAsync(dto, c),
                    c => searchIndexService.DeleteApplicationAsync(msg.EntityId, c));
                break;

            case IndexEntityType.ServiceAccount:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.ServiceAccounts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    sa => new ServiceAccountDto(sa.Id, sa.Name, sa.Description, sa.ClientId, sa.IsActive, sa.Audiences, sa.AccessTokenLifetimeMinutes),
                    (dto, c) => searchIndexService.IndexServiceAccountAsync(dto, c),
                    c => searchIndexService.DeleteServiceAccountAsync(msg.EntityId, c));
                break;

            case IndexEntityType.NotificationTemplate:
                await IndexOrDeleteAsync(msg, ct, dbContext,
                    db => db.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(e => e.Id == msg.EntityId, ct),
                    t => new NotificationTemplateDto(t.Id, t.Type.ToString(), t.Locale, t.Subject, t.Body),
                    (dto, c) => searchIndexService.IndexNotificationTemplateAsync(dto, c),
                    c => searchIndexService.DeleteNotificationTemplateAsync(msg.EntityId, c));
                break;

            default:
                logger.LogWarning("Unknown entity type {EntityType} for IndexEntityRequested", msg.EntityType);
                break;
        }
    }

    private async Task IndexOrDeleteAsync<TEntity, TDto>(
        IndexEntityRequested msg, CancellationToken ct, AuthDbContext dbContext,
        Func<AuthDbContext, Task<TEntity?>> loader,
        Func<TEntity, TDto> mapper,
        Func<TDto, CancellationToken, Task> indexer,
        Func<CancellationToken, Task> deleter)
        where TEntity : class
        where TDto : class
    {
        if (msg.Operation == IndexOperation.Delete)
        {
            await deleter(ct);
            return;
        }

        var entity = await loader(dbContext);
        if (entity is null)
        {
            logger.LogWarning("Entity {EntityType}/{EntityId} not found, skipping indexing", msg.EntityType, msg.EntityId);
            return;
        }

        await indexer(mapper(entity), ct);
    }
}

internal sealed class IndexEntityConsumerDefinition : ConsumerDefinition<IndexEntityConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<IndexEntityConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5)));
    }
}

