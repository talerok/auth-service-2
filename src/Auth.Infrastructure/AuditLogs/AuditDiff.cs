using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Auth.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Auth.Infrastructure.AuditLogs;

public static class AuditDiff
{
    private static readonly ConcurrentDictionary<Type, HashSet<string>> Cache = new();

    private static HashSet<string> GetAuditableFields(Type type) =>
        Cache.GetOrAdd(type, t => t.GetProperties()
            .Where(p => p.GetCustomAttribute<AuditableAttribute>() is not null)
            .Select(p => p.Name)
            .ToHashSet());

    public static Dictionary<string, object?> CaptureState(EntityEntry entry)
    {
        var allowed = GetAuditableFields(entry.Entity.GetType());
        if (allowed.Count == 0) return new();

        return entry.Properties
            .Where(p => allowed.Contains(p.Metadata.Name))
            .ToDictionary(
                p => JsonNamingPolicy.CamelCase.ConvertName(p.Metadata.Name),
                p => p.CurrentValue);
    }

    public static Dictionary<string, object?> CaptureChanges(EntityEntry entry)
    {
        var allowed = GetAuditableFields(entry.Entity.GetType());
        if (allowed.Count == 0) return new();

        var changes = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (!allowed.Contains(prop.Metadata.Name) || !prop.IsModified) continue;
            if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;
            changes[JsonNamingPolicy.CamelCase.ConvertName(prop.Metadata.Name)] = new Dictionary<string, object?>
            {
                ["old"] = prop.OriginalValue,
                ["new"] = prop.CurrentValue
            };
        }
        return changes;
    }
}
