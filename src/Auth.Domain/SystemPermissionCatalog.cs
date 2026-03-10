namespace Auth.Domain;

public static class SystemPermissionCatalog
{
    public static readonly (string Domain, int Bit, string Code, string Description)[] Permissions =
    [
        // system.users
        ("system.users", 0, "view", "View users"),
        ("system.users", 1, "create", "Create users"),
        ("system.users", 2, "update", "Update users"),
        ("system.users", 3, "delete", "Delete users"),
        ("system.users", 4, "reset-password", "Reset user password"),
        ("system.users", 5, "import", "Import users"),
        ("system.users", 6, "export", "Export users"),

        // system.roles
        ("system.roles", 0, "view", "View roles"),
        ("system.roles", 1, "create", "Create roles"),
        ("system.roles", 2, "update", "Update roles"),
        ("system.roles", 3, "delete", "Delete roles"),
        ("system.roles", 4, "import", "Import roles"),
        ("system.roles", 5, "export", "Export roles"),

        // system.permissions
        ("system.permissions", 0, "view", "View permissions"),
        ("system.permissions", 1, "create", "Create permissions"),
        ("system.permissions", 2, "update", "Update permissions"),
        ("system.permissions", 3, "delete", "Delete permissions"),
        ("system.permissions", 4, "import", "Import permissions"),
        ("system.permissions", 5, "export", "Export permissions"),

        // system.workspaces
        ("system.workspaces", 0, "view", "View workspaces"),
        ("system.workspaces", 1, "create", "Create workspaces"),
        ("system.workspaces", 2, "update", "Update workspaces"),
        ("system.workspaces", 3, "delete", "Delete workspaces"),
        ("system.workspaces", 4, "import", "Import workspaces"),
        ("system.workspaces", 5, "export", "Export workspaces"),

        // system.search
        ("system.search", 0, "reindex", "Reindex search"),

        // system.notification-templates
        ("system.notification-templates", 0, "view", "View notification templates"),
        ("system.notification-templates", 1, "update", "Update notification templates"),

        // system.identity-sources
        ("system.identity-sources", 0, "view", "View identity sources"),
        ("system.identity-sources", 1, "create", "Create identity sources"),
        ("system.identity-sources", 2, "update", "Update identity sources"),
        ("system.identity-sources", 3, "delete", "Delete identity sources"),

        // system.api-clients
        ("system.api-clients", 0, "view", "View API clients"),
        ("system.api-clients", 1, "create", "Create API clients"),
        ("system.api-clients", 2, "update", "Update API clients"),
        ("system.api-clients", 3, "delete", "Delete API clients")
    ];
}
