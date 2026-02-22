namespace Auth.Application;

public sealed record UserDto(Guid Id, string Username, string Email, bool IsActive, bool MustChangePassword = false);
public sealed record CreateUserRequest(string Username, string Email, string Password, bool IsActive = true, bool MustChangePassword = false);
public sealed record UpdateUserRequest(string Username, string Email, bool IsActive, bool MustChangePassword = false);
public sealed record PatchUserRequest(string? Username, string? Email, bool? IsActive, bool? MustChangePassword = null);
