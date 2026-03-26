using System.Text.Json.Serialization;
using System.Text.Json;

namespace Auth.Application;

public sealed record SetIdsRequest(IReadOnlyCollection<Guid> Ids);
public sealed record SetRolesRequest(IReadOnlyCollection<RoleDto> Roles);
public sealed record SetPermissionsRequest(IReadOnlyCollection<PermissionDto> Permissions);
public sealed record UserWorkspaceRolesItem(Guid WorkSpaceId, IReadOnlyCollection<Guid> RoleIds);
public sealed record SetUserWorkspacesRequest(IReadOnlyCollection<UserWorkspaceRolesItem> Workspaces);
public sealed record UserIdentitySourceLinkItem(Guid IdentitySourceId, string ExternalIdentity);
public sealed record SetUserIdentitySourceLinksRequest(IReadOnlyCollection<UserIdentitySourceLinkItem> Links);

public sealed record SearchFieldFilter(
    [property: JsonPropertyName("eq")] string? Eq = null,
    [property: JsonPropertyName("in")] IReadOnlyCollection<string>? In = null,
    [property: JsonPropertyName("ts")] string? Ts = null,
    [property: JsonPropertyName("from")] string? From = null,
    [property: JsonPropertyName("to")] string? To = null);

[JsonConverter(typeof(SearchSortOrderJsonConverter))]
public enum SearchSortOrder
{
    Asc = 1,
    Desc = 2
}

public sealed record SearchRequest(
    [property: JsonPropertyName("filter")] IReadOnlyDictionary<string, SearchFieldFilter>? Filter,
    [property: JsonPropertyName("sortBy")] string? SortBy,
    [property: JsonPropertyName("sortOrder")] SearchSortOrder SortOrder = SearchSortOrder.Asc,
    [property: JsonPropertyName("page")] int Page = 1,
    [property: JsonPropertyName("pageSize")] int PageSize = 20);

public sealed record SearchResponse<TItem>(IReadOnlyCollection<TItem> Items, int Page, int PageSize, long TotalCount);

public sealed class SearchSortOrderJsonConverter : JsonConverter<SearchSortOrder>
{
    public override SearchSortOrder Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "ASC" => SearchSortOrder.Asc,
            "DESC" => SearchSortOrder.Desc,
            _ => throw new JsonException("sortOrder must be ASC or DESC")
        };
    }

    public override void Write(Utf8JsonWriter writer, SearchSortOrder value, JsonSerializerOptions options)
    {
        var serialized = value switch
        {
            SearchSortOrder.Asc => "ASC",
            SearchSortOrder.Desc => "DESC",
            _ => throw new JsonException("sortOrder must be ASC or DESC")
        };

        writer.WriteStringValue(serialized);
    }
}
