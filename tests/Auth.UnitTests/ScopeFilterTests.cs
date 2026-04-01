using Auth.Application.Common;
using FluentAssertions;

namespace Auth.UnitTests;

public sealed class ScopeFilterTests
{
    [Fact]
    public void Filter_PassthroughScopes_AlwaysIncluded()
    {
        var requested = new[] { "openid", "offline_access", "email" };
        var allowed = new[] { "email", "profile" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().Contain("openid");
        result.Should().Contain("offline_access");
        result.Should().Contain("email");
    }

    [Fact]
    public void Filter_PassthroughScopes_IncludedEvenIfNotInAllowed()
    {
        var requested = new[] { "openid", "offline_access" };
        var allowed = new[] { "email" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().BeEquivalentTo("openid", "offline_access");
    }

    [Fact]
    public void Filter_DisallowedScope_Excluded()
    {
        var requested = new[] { "openid", "email", "phone" };
        var allowed = new[] { "email", "profile" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().Contain("email");
        result.Should().NotContain("phone");
        result.Should().NotContain("profile");
    }

    [Fact]
    public void Filter_WildcardWorkspaceScope_AllowsAnyConcrete()
    {
        var requested = new[] { "openid", "ws:finance", "ws:hr" };
        var allowed = new[] { "email", "ws:*" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().Contain("ws:finance");
        result.Should().Contain("ws:hr");
    }

    [Fact]
    public void Filter_ConcreteWorkspaceScopes_OnlyAllowedPass()
    {
        var requested = new[] { "openid", "ws:finance", "ws:hr" };
        var allowed = new[] { "ws:finance" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().Contain("ws:finance");
        result.Should().NotContain("ws:hr");
    }

    [Fact]
    public void Filter_EmptyAllowedScopes_ReturnsEmpty()
    {
        var requested = new[] { "openid", "email" };
        var allowed = Array.Empty<string>();

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Filter_EmptyRequestedScopes_ReturnsEmpty()
    {
        var requested = Array.Empty<string>();
        var allowed = new[] { "email", "profile" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Filter_WildcardInAllowed_AlsoPassesWildcardItself()
    {
        var requested = new[] { "ws:*", "ws:finance" };
        var allowed = new[] { "ws:*" };

        var result = ScopeFilter.Filter(requested, allowed);

        result.Should().Contain("ws:*");
        result.Should().Contain("ws:finance");
    }
}
