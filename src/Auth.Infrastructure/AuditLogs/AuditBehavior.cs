using Auth.Application;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.AuditLogs;

internal sealed class AuditBehavior<TRequest, TResponse>(
    IAuditService auditService,
    IAuditContext auditContext,
    AuthDbContext dbContext,
    ILogger<AuditBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditable
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.Critical)
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var response = await next();
                await WriteAudit(request, response, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return response;
            }
            catch (Exception) when (auditContext.Details is not null)
            {
                try
                {
                    await WriteFailureAudit(request, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to write failure audit for {EntityType}.{Action}",
                        request.EntityType, request.Action);
                }

                throw;
            }
        }
        else
        {
            var response = await next();
            try
            {
                await WriteAudit(request, response, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Audit log failed for {EntityType}.{Action}", request.EntityType, request.Action);
            }
            return response;
        }
    }

    private async Task WriteAudit(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        var entityId = auditContext.EntityId ?? request.EntityId;
        var details = auditContext.Details;

        AuditActor? actor = auditContext.Actor
            ?? (response is IAuditActorProvider actorProvider ? actorProvider.GetAuditActor() : null);

        await auditService.LogAsync(
            request.EntityType, entityId, request.Action,
            details, actor, request.Critical, cancellationToken);
    }

    private async Task WriteFailureAudit(TRequest request, CancellationToken cancellationToken)
    {
        var entityId = auditContext.EntityId ?? request.EntityId;
        var details = auditContext.Details;

        await auditService.LogAsync(
            request.EntityType, entityId, request.Action,
            details, auditContext.Actor, request.Critical, cancellationToken);
    }
}
