using Auth.Application;
using Auth.Application.IdentitySources.Commands.DeleteIdentitySource;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.IdentitySources.Commands.DeleteIdentitySource;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Auth.UnitTests.TestDbContextFactory;

namespace Auth.UnitTests.IdentitySources.Commands;

public sealed class DeleteIdentitySourceCommandHandlerTests
{
    [Fact]
    public async Task Handle_SoftDeletesSource()
    {
        await using var dbContext = CreateDbContext();
        var source = new IdentitySource { Name = "keycloak", Code = "keycloak", DisplayName = "Keycloak", Type = IdentitySourceType.Oidc, IsEnabled = true };
        dbContext.IdentitySources.Add(source);
        await dbContext.SaveChangesAsync();
        var handler = new DeleteIdentitySourceCommandHandler(dbContext);

        await handler.Handle(new DeleteIdentitySourceCommand(source.Id), CancellationToken.None);

        var deleted = await dbContext.IdentitySources.IgnoreQueryFilters().FirstAsync(x => x.Id == source.Id);
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsException()
    {
        await using var dbContext = CreateDbContext();
        var handler = new DeleteIdentitySourceCommandHandler(dbContext);

        var act = () => handler.Handle(new DeleteIdentitySourceCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.IdentitySourceNotFound);
    }

}
