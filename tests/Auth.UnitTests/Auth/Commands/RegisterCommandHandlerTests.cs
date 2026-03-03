using Auth.Application;
using Auth.Application.Auth.Commands.Register;
using Auth.Domain;
using Auth.Infrastructure;
using Auth.Infrastructure.Authentication.Commands.Register;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Auth.UnitTests.Auth.Commands;

public sealed class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesUserAndIndexes()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(x => x.Hash("password123")).Returns("hashed_password");
        var searchIndex = new Mock<ISearchIndexService>();
        var handler = new RegisterCommandHandler(dbContext, hasher.Object, searchIndex.Object);

        var result = await handler.Handle(
            new RegisterCommand("bob", "Bob Smith", "bob@example.com", "password123"),
            CancellationToken.None);

        result.Username.Should().Be("bob");
        result.FullName.Should().Be("Bob Smith");
        result.Email.Should().Be("bob@example.com");
        result.IsActive.Should().BeTrue();

        var user = await dbContext.Users.FirstAsync(x => x.Id == result.Id);
        user.PasswordHash.Should().Be("hashed_password");

        searchIndex.Verify(x => x.IndexUserAsync(
            It.Is<UserDto>(dto => dto.Id == result.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsDuplicateIdentity()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Mock<IPasswordHasher>();
        var searchIndex = new Mock<ISearchIndexService>();
        var existing = new User { Username = "bob", Email = "existing@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(existing);
        await dbContext.SaveChangesAsync();
        var handler = new RegisterCommandHandler(dbContext, hasher.Object, searchIndex.Object);

        var act = () => handler.Handle(
            new RegisterCommand("bob", "Bob Smith", "bob-new@example.com", "password123"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthException>()
            .Where(x => x.Code == AuthErrorCatalog.DuplicateIdentity);
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthDbContext(options);
    }
}
