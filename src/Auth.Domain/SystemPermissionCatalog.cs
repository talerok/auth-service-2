namespace Auth.Domain;

public static class SystemPermissionCatalog
{
    public static readonly (int Bit, string Code, string Description)[] Permissions =
    [
        (0, "users.view", "View users"),
        (1, "users.create", "Create users"),
        (2, "users.update", "Update users"),
        (3, "users.delete", "Delete users"),
        (4, "roles.view", "View roles"),
        (5, "roles.create", "Create roles"),
        (6, "roles.update", "Update roles"),
        (7, "roles.delete", "Delete roles"),
        (8, "permissions.view", "View permissions"),
        (9, "permissions.create", "Create permissions"),
        (10, "permissions.update", "Update permissions"),
        (11, "permissions.delete", "Delete permissions"),
        (12, "workspaces.view", "View workspaces"),
        (13, "workspaces.create", "Create workspaces"),
        (14, "workspaces.update", "Update workspaces"),
        (15, "workspaces.delete", "Delete workspaces")
    ];
}
