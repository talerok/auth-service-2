namespace Auth.Domain;

public static class SystemPermissionCatalog
{
    public static readonly (string Domain, int Bit, string Code, string Description)[] Permissions =
    [
        // system.users
        ("system", 0, "system.users.view", "View users"),
        ("system", 1, "system.users.create", "Create users"),
        ("system", 2, "system.users.update", "Update users"),
        ("system", 3, "system.users.delete", "Delete users"),
        ("system", 4, "system.users.reset-password", "Reset user password"),
        ("system", 5, "system.users.import", "Import users"),
        ("system", 6, "system.users.export", "Export users"),

        // system.roles
        ("system", 7, "system.roles.view", "View roles"),
        ("system", 8, "system.roles.create", "Create roles"),
        ("system", 9, "system.roles.update", "Update roles"),
        ("system", 10, "system.roles.delete", "Delete roles"),
        ("system", 11, "system.roles.import", "Import roles"),
        ("system", 12, "system.roles.export", "Export roles"),

        // system.permissions
        ("system", 13, "system.permissions.view", "View permissions"),
        ("system", 14, "system.permissions.create", "Create permissions"),
        ("system", 15, "system.permissions.update", "Update permissions"),
        ("system", 16, "system.permissions.delete", "Delete permissions"),
        ("system", 17, "system.permissions.import", "Import permissions"),
        ("system", 18, "system.permissions.export", "Export permissions"),

        // system.workspaces
        ("system", 19, "system.workspaces.view", "View workspaces"),
        ("system", 20, "system.workspaces.create", "Create workspaces"),
        ("system", 21, "system.workspaces.update", "Update workspaces"),
        ("system", 22, "system.workspaces.delete", "Delete workspaces"),
        ("system", 23, "system.workspaces.import", "Import workspaces"),
        ("system", 24, "system.workspaces.export", "Export workspaces"),

        // system.search
        ("system", 25, "system.search.reindex", "Reindex search"),

        // system.notification-templates
        ("system", 26, "system.notification-templates.view", "View notification templates"),
        ("system", 27, "system.notification-templates.create", "Create notification templates"),
        ("system", 28, "system.notification-templates.update", "Update notification templates"),
        ("system", 29, "system.notification-templates.delete", "Delete notification templates"),

        // system.identity-sources
        ("system", 30, "system.identity-sources.view", "View identity sources"),
        ("system", 31, "system.identity-sources.create", "Create identity sources"),
        ("system", 32, "system.identity-sources.update", "Update identity sources"),
        ("system", 33, "system.identity-sources.delete", "Delete identity sources"),

        // system.applications
        ("system", 34, "system.applications.view", "View applications"),
        ("system", 35, "system.applications.create", "Create applications"),
        ("system", 36, "system.applications.update", "Update applications"),
        ("system", 37, "system.applications.delete", "Delete applications"),

        // system.service-accounts
        ("system", 38, "system.service-accounts.view", "View service accounts"),
        ("system", 39, "system.service-accounts.create", "Create service accounts"),
        ("system", 40, "system.service-accounts.update", "Update service accounts"),
        ("system", 41, "system.service-accounts.delete", "Delete service accounts"),

        // system.audit-logs
        ("system", 42, "system.audit-logs.view", "View audit logs")
    ];
}
