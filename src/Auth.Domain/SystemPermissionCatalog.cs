namespace Auth.Domain;

public static class SystemPermissionCatalog
{
    /// <summary>
    /// Биты 0–127 зарезервированы для системных полномочий.
    /// Пользовательские полномочия начинаются с этого бита.
    /// </summary>
    public const int CustomBitStart = 128;

    public static readonly (int Bit, string Code, string Description)[] Permissions =
    [
        (0, "system.users.view", "View users"),
        (1, "system.users.create", "Create users"),
        (2, "system.users.update", "Update users"),
        (3, "system.users.delete", "Delete users"),
        (4, "system.roles.view", "View roles"),
        (5, "system.roles.create", "Create roles"),
        (6, "system.roles.update", "Update roles"),
        (7, "system.roles.delete", "Delete roles"),
        (8, "system.permissions.view", "View permissions"),
        (9, "system.permissions.create", "Create permissions"),
        (10, "system.permissions.update", "Update permissions"),
        (11, "system.permissions.delete", "Delete permissions"),
        (12, "system.workspaces.view", "View workspaces"),
        (13, "system.workspaces.create", "Create workspaces"),
        (14, "system.workspaces.update", "Update workspaces"),
        (15, "system.workspaces.delete", "Delete workspaces"),
        (16, "system.search.reindex", "Reindex search"),
        (17, "system.users.reset-password", "Reset user password"),
        (18, "system.notification-templates.view", "View notification templates"),
        (19, "system.notification-templates.update", "Update notification templates"),
        (20, "system.identity-sources.view", "View identity sources"),
        (21, "system.identity-sources.create", "Create identity sources"),
        (22, "system.identity-sources.update", "Update identity sources"),
        (23, "system.identity-sources.delete", "Delete identity sources"),
        (24, "system.api-clients.view", "View API clients"),
        (25, "system.api-clients.create", "Create API clients"),
        (26, "system.api-clients.update", "Update API clients"),
        (27, "system.api-clients.delete", "Delete API clients")
    ];
}
