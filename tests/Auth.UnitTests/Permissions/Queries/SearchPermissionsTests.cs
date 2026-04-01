using Auth.Application;
using Auth.Application.Permissions.Queries.SearchPermissions;
using Auth.Infrastructure.Permissions.Queries.SearchPermissions;
using FluentAssertions;
using Moq;

namespace Auth.UnitTests.Permissions.Queries;

public sealed class SearchPermissionsTests
{
    [Fact]
    public async Task Search_DelegatesToSearchService()
    {
        var searchService = new Mock<ISearchService>();
        var expectedItems = new List<PermissionDto>
        {
            new(Guid.NewGuid(), "test.domain", 0, "perm.a", "A", false),
            new(Guid.NewGuid(), "test.domain", 1, "perm.b", "B", true)
        };
        var expectedResponse = new SearchResponse<PermissionDto>(expectedItems, 1, 20, 2);
        var request = new SearchRequest(null, null);
        searchService
            .Setup(x => x.SearchPermissionsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        var handler = new SearchPermissionsQueryHandler(searchService.Object);

        var result = await handler.Handle(
            new SearchPermissionsQuery(request),
            CancellationToken.None);

        result.Should().BeSameAs(expectedResponse);
        searchService.Verify(
            x => x.SearchPermissionsAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
