using Auth.Application;
using Auth.Application.Users.Commands.SetUserIdentitySourceLinks;
using Auth.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Users.Commands.SetUserIdentitySourceLinks;

internal sealed class SetUserIdentitySourceLinksCommandHandler(
    AuthDbContext dbContext,
    IAuditContext auditContext) : IRequestHandler<SetUserIdentitySourceLinksCommand>
{
    public async Task Handle(SetUserIdentitySourceLinksCommand command, CancellationToken cancellationToken)
    {
        var desiredBySourceId = command.Links
            .GroupBy(x => x.IdentitySourceId)
            .ToDictionary(
                x => x.Key,
                x => x.First().ExternalIdentity);

        var sourceIds = desiredBySourceId.Keys.ToArray();
        var current = await dbContext.IdentitySourceLinks
            .Where(x => x.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        var currentSourceIds = current.Select(x => x.IdentitySourceId).ToArray();
        var diff = CollectionDiff.Calculate(sourceIds, currentSourceIds);

        var toRemove = current.Where(x => diff.ToRemove.Contains(x.IdentitySourceId)).ToArray();
        if (toRemove.Length > 0)
            dbContext.IdentitySourceLinks.RemoveRange(toRemove);

        foreach (var sourceId in diff.ToAdd)
        {
            dbContext.IdentitySourceLinks.Add(new IdentitySourceLink
            {
                UserId = command.UserId,
                IdentitySourceId = sourceId,
                ExternalIdentity = desiredBySourceId[sourceId]
            });
        }

        // Update ExternalIdentity for existing links if changed
        foreach (var link in current.Where(x => !diff.ToRemove.Contains(x.IdentitySourceId)))
        {
            var desired = desiredBySourceId[link.IdentitySourceId];
            if (link.ExternalIdentity != desired)
                link.ExternalIdentity = desired;
        }

        auditContext.Details = new Dictionary<string, object?>
        {
            ["added"] = diff.ToAdd.ToList(),
            ["removed"] = diff.ToRemove.ToList()
        };

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
