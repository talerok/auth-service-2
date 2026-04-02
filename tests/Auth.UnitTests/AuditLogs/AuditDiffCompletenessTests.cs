using System.Reflection;
using Auth.Domain;
using FluentAssertions;

namespace Auth.UnitTests.AuditLogs;

public sealed class AuditDiffCompletenessTests
{
    private static readonly HashSet<string> EntityBaseProperties =
        typeof(EntityBase)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet();

    private static readonly Dictionary<Type, HashSet<string>> ExcludedProperties = new()
    {
        [typeof(User)] = ["PasswordHash", "PasswordChangedAt", "UserWorkspaces", "TwoFactorChallenges", "FailedLoginAttempts", "LockoutEndTime", "IsLockedOut"],
        [typeof(Role)] = ["UserWorkspaceRoles", "RolePermissions"],
        [typeof(Permission)] = ["RolePermissions"],
        [typeof(Workspace)] = ["UserWorkspaces"],
        [typeof(IdentitySource)] = ["OidcConfig", "LdapConfig", "Links"],
        [typeof(ServiceAccount)] = ["ServiceAccountWorkspaces"],
        [typeof(Domain.Application)] = [],
        [typeof(NotificationTemplate)] = ["Body"]
    };

    public static IEnumerable<object[]> AuditableEntities()
    {
        var domainAssembly = typeof(EntityBase).Assembly;

        return domainAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetProperties().Any(p => p.GetCustomAttribute<AuditableAttribute>() is not null))
            .Select(t => new object[] { t });
    }

    [Theory]
    [MemberData(nameof(AuditableEntities))]
    public void AllPublicProperties_ShouldBeExplicitlyAuditableOrExcluded(Type entityType)
    {
        ExcludedProperties.Should().ContainKey(entityType,
            $"entity {entityType.Name} has [Auditable] properties but no exclusion set defined in this test — " +
            "add it to ExcludedProperties");

        var excluded = ExcludedProperties[entityType];

        var properties = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !EntityBaseProperties.Contains(p.Name))
            .Where(p => !IsCollectionNavigation(p))
            .ToList();

        foreach (var prop in properties)
        {
            var hasAuditable = prop.GetCustomAttribute<AuditableAttribute>() is not null;
            var isExcluded = excluded.Contains(prop.Name);

            (hasAuditable || isExcluded).Should().BeTrue(
                $"property '{entityType.Name}.{prop.Name}' is neither marked with [Auditable] " +
                "nor listed in the exclusion set — decide whether it should be audited");
        }
    }

    private static bool IsCollectionNavigation(PropertyInfo prop)
    {
        if (!prop.PropertyType.IsGenericType) return false;
        var genericDef = prop.PropertyType.GetGenericTypeDefinition();
        return genericDef == typeof(ICollection<>) || genericDef == typeof(List<>);
    }
}
