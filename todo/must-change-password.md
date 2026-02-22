# Task: Forced Password Change on Login

## Context

Add a `MustChangePassword` flag to users. When set, login returns 200 with a
temporary challenge ID instead of tokens. The client uses that ID to call a
forced-change endpoint, which updates the password and returns real tokens.

### Flow

```
POST /api/auth/login
  credentials valid + user.MustChangePassword == true
  → create PasswordChangeChallenge (TTL from config, default 15 min)
  → 200: { requiresPasswordChange: true, passwordChangeChallengeId: <guid> }

POST /api/auth/password/forced-change
  { challengeId: <guid>, newPassword: "..." }
  → validate challenge (exists, not used, not expired)
  → update PasswordHash, clear MustChangePassword flag
  → mark challenge as used
  → issue AccessToken + RefreshToken
  → 200: AuthTokensResponse
```

The `challengeId` (Guid) is the opaque one-time token returned to the client.
Security relies on unpredictability (UUID v4), single use, and TTL.

**Priority rule:** check `MustChangePassword` AFTER credential validation and
IsActive check, but BEFORE the 2FA check. If both flags are set, forced
password change wins — skip 2FA entirely.

---

## Implementation Steps

### Step 1 — Domain: User entity

**File:** `src/Auth.Domain/User.cs`

Add property:
```csharp
public bool MustChangePassword { get; private set; }
```

Add methods:
```csharp
public void MarkMustChangePassword()
{
    MustChangePassword = true;
    UpdatedAt = DateTime.UtcNow;
}

public void ClearMustChangePassword()
{
    MustChangePassword = false;
    UpdatedAt = DateTime.UtcNow;
}
```

---

### Step 2 — Domain: new entity PasswordChangeChallenge

**File:** `src/Auth.Domain/PasswordChangeChallenge.cs` (create new)

```csharp
namespace Auth.Domain;

public sealed class PasswordChangeChallenge
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; private set; }
    public User? User { get; private set; }

    private PasswordChangeChallenge() { }

    public static PasswordChangeChallenge Create(Guid userId, DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt));

        return new PasswordChangeChallenge
        {
            UserId = userId,
            ExpiresAt = expiresAt
        };
    }

    public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

    public void MarkAsUsed()
    {
        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }
}
```

---

### Step 3 — Application: contracts

**File:** `src/Auth.Application/Auth/AuthContracts.cs`

1. Extend `LoginResponse` — add two fields:
   - `bool RequiresPasswordChange`
   - `Guid? PasswordChangeChallengeId`

   Update all existing `new LoginResponse(...)` call sites to pass the new
   fields (use `false` and `null` in the places that don't need them).

2. Add new record:
```csharp
public sealed record ForcedPasswordChangeRequest(Guid ChallengeId, string NewPassword);
```

---

### Step 4 — Application: error codes

**File:** `src/Auth.Application/Common/AuthErrorCatalog.cs`

Add:
```csharp
public const string PasswordChangeRequired = "AUTH_PASSWORD_CHANGE_REQUIRED";
public const string InvalidPasswordChangeChallenge = "AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE";
```

---

### Step 5 — Application: IAuthService interface

**File:** `src/Auth.Application/Abstractions/IAuthService.cs`

Add method:
```csharp
Task<AuthTokensResponse> ForcedChangePasswordAsync(
    ForcedPasswordChangeRequest request, CancellationToken cancellationToken);
```

---

### Step 6 — Application: user contracts

**File:** `src/Auth.Application/Users/UserContracts.cs`

- `UserDto` — add `bool MustChangePassword`
- `CreateUserRequest` — add `bool MustChangePassword = false`
- `UpdateUserRequest` — add `bool MustChangePassword`
- `PatchUserRequest` — add `bool? MustChangePassword`

Update all `new UserDto(...)` call sites to pass the new field.

---

### Step 7 — Infrastructure: configuration option

**File:** `src/Auth.Infrastructure/Configuration/IntegrationOptions.cs` (or
wherever `TwoFactorOptions` lives)

Add to the appropriate options class (or create `PasswordChangeOptions`):
```csharp
public int PasswordChangeTtlMinutes { get; set; } = 15;
```

Register in DI and bind from environment/config.

---

### Step 8 — Infrastructure: EF Core DbContext

**File:** `src/Auth.Infrastructure/AuthDbContext.cs`

1. Add:
```csharp
public DbSet<PasswordChangeChallenge> PasswordChangeChallenges => Set<PasswordChangeChallenge>();
```

2. In `OnModelCreating`, add configuration for `PasswordChangeChallenge`:
```csharp
builder.Entity<PasswordChangeChallenge>(e =>
{
    e.ToTable("password_change_challenges");
    e.HasKey(x => x.Id);
    e.Property(x => x.UserId).IsRequired();
    e.Property(x => x.ExpiresAt).IsRequired();
    e.Property(x => x.IsUsed).IsRequired().HasDefaultValue(false);
    e.Property(x => x.CreatedAt).IsRequired();
    e.Property(x => x.UsedAt);
    e.HasOne(x => x.User)
     .WithMany()
     .HasForeignKey(x => x.UserId)
     .OnDelete(DeleteBehavior.Cascade);
    e.HasIndex(x => x.UserId);
});
```

3. Add configuration for the new `User.MustChangePassword` column:
```csharp
// In the Users entity config:
e.Property(x => x.MustChangePassword).IsRequired().HasDefaultValue(false);
```

---

### Step 9 — Infrastructure: EF migration

Run:
```bash
dotnet ef migrations add AddMustChangePasswordSupport \
  --project src/Auth.Infrastructure \
  --startup-project src/Auth.Api
```

The migration must produce:
- Column `must_change_password` (boolean, NOT NULL, DEFAULT false) on table `users`
- New table `password_change_challenges`:
  - `id` uuid PRIMARY KEY
  - `user_id` uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE
  - `expires_at` timestamptz NOT NULL
  - `is_used` boolean NOT NULL DEFAULT false
  - `created_at` timestamptz NOT NULL
  - `used_at` timestamptz NULL
  - INDEX on `user_id`

Verify the generated migration file matches the above before proceeding.

---

### Step 10 — Infrastructure: AuthService — modify LoginAsync

**File:** `src/Auth.Infrastructure/Services/AuthService.cs`

In `LoginAsync`, after the `IsActive` check and before the `TwoFactorEnabled`
check, insert:

```csharp
if (user.MustChangePassword)
{
    var challenge = PasswordChangeChallenge.Create(
        user.Id,
        DateTime.UtcNow.AddMinutes(_options.PasswordChangeTtlMinutes));
    dbContext.PasswordChangeChallenges.Add(challenge);
    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
        "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
        user.Id, "PASSWORD_CHANGE_CHALLENGE_CREATED", "SUCCESS");

    return new LoginResponse(
        requiresTwoFactor: false,
        tokens: null,
        challengeId: null,
        channel: null,
        requiresPasswordChange: true,
        passwordChangeChallengeId: challenge.Id);
}
```

---

### Step 11 — Infrastructure: AuthService — implement ForcedChangePasswordAsync

**File:** `src/Auth.Infrastructure/Services/AuthService.cs`

Add method:

```csharp
public async Task<AuthTokensResponse> ForcedChangePasswordAsync(
    ForcedPasswordChangeRequest request, CancellationToken cancellationToken)
{
    var challenge = await dbContext.PasswordChangeChallenges
        .FirstOrDefaultAsync(x => x.Id == request.ChallengeId, cancellationToken);

    if (challenge is null || challenge.IsUsed || challenge.IsExpired(DateTime.UtcNow))
        throw new AuthException(AuthErrorCatalog.InvalidPasswordChangeChallenge);

    var user = await dbContext.Users
        .FirstOrDefaultAsync(x => x.Id == challenge.UserId, cancellationToken);

    if (user is null || !user.IsActive)
        throw new AuthException(AuthErrorCatalog.UserInactive);

    user.PasswordHash = passwordHasher.Hash(request.NewPassword);
    user.ClearMustChangePassword();
    challenge.MarkAsUsed();

    var masks = await BuildWorkspaceMasksAsync(user.Id, cancellationToken);
    var tokens = jwtTokenFactory.CreateTokens(user, masks);
    await SaveRefreshTokenAsync(user.Id, tokens.RefreshToken, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);

    logger.LogInformation(
        "PasswordChangeOperation userId={UserId} operation={Operation} result={Result}",
        user.Id, "FORCED_PASSWORD_CHANGED", "SUCCESS");

    return tokens;
}
```

---

### Step 12 — Infrastructure: UserService — MustChangePassword flag

**File:** `src/Auth.Infrastructure/Services/UserService.cs`

- In the `UpdateAsync` / `PatchAsync` methods, apply `MustChangePassword` from
  the request to the user entity using `user.MarkMustChangePassword()` or
  `user.ClearMustChangePassword()`.
- In the mapper from `User` to `UserDto`, pass `user.MustChangePassword`.

---

### Step 13 — Infrastructure: error mapping

**File:** wherever `AuthProblemDetailsMapper` (or equivalent) maps error codes
to HTTP responses.

Add mappings:
```csharp
AuthErrorCatalog.InvalidPasswordChangeChallenge =>
    (StatusCodes.Status401Unauthorized, "Invalid or expired password change token"),
```

---

### Step 14 — API: controller

**File:** `src/Auth.Api/Controllers/AuthController.cs`

Add endpoint:
```csharp
[HttpPost("password/forced-change")]
[AllowAnonymous]
[ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<AuthTokensResponse>> ForcedChangePassword(
    [FromBody] ForcedPasswordChangeRequest request,
    CancellationToken cancellationToken) =>
    Ok(await authService.ForcedChangePasswordAsync(request, cancellationToken));
```

---

### Step 15 — Docs: api.md

**File:** `docs/api.md`

1. Update `POST /api/auth/login` — add second response example showing the
   password-change scenario:

```json
// When MustChangePassword == true
{
  "requiresTwoFactor": false,
  "requiresPasswordChange": true,
  "passwordChangeChallengeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tokens": null,
  "challengeId": null,
  "channel": null
}
```

2. Add new section `POST /api/auth/password/forced-change`:

```
POST /api/auth/password/forced-change
AllowAnonymous

Request:
{
  "challengeId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "newPassword": "NewSecurePassword1!"
}

Response 200 OK:
{
  "accessToken": "<jwt>",
  "refreshToken": "<refresh-token>",
  "expiresAt": "2026-01-01T12:15:00Z"
}

Response 401 Unauthorized:
{
  "code": "AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE",
  ...
}
```

3. Add new error code to the errors table:
   - `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE` → 401

---

### Step 16 — Docs: database.md

**File:** `docs/database.md`

1. Add `must_change_password` column to the `users` table description.

2. Add new table `password_change_challenges`:

| Column      | Type        | Nullable | Description                          |
|-------------|-------------|----------|--------------------------------------|
| id          | uuid        | NO       | PK; also serves as the one-time token|
| user_id     | uuid        | NO       | FK → users(id) CASCADE               |
| expires_at  | timestamptz | NO       | Challenge TTL (default 15 min)       |
| is_used     | boolean     | NO       | Consumed flag                        |
| created_at  | timestamptz | NO       |                                      |
| used_at     | timestamptz | YES      | When the challenge was consumed      |

---

### Step 17 — Unit tests

**File:** `tests/Auth.UnitTests/AuthServiceTests.cs`

Add the following test cases. Follow the existing Arrange-Act-Assert structure
and naming convention `{Method}_{Scenario}_{ExpectedResult}`.

#### LoginAsync tests

- `LoginAsync_WhenMustChangePassword_ReturnsFlagTrue`
  - Arrange: user with `MustChangePassword = true`, valid credentials
  - Assert: `result.RequiresPasswordChange == true`

- `LoginAsync_WhenMustChangePassword_ReturnsNullTokens`
  - Assert: `result.Tokens == null`

- `LoginAsync_WhenMustChangePassword_ReturnsNonNullChallengeId`
  - Assert: `result.PasswordChangeChallengeId != null`

- `LoginAsync_WhenMustChangePasswordAndTwoFactorEnabled_DoesNotCreateTwoFactorChallenge`
  - Arrange: user with both flags set
  - Assert: no `TwoFactorChallenge` created; `RequiresPasswordChange == true`

- `LoginAsync_WhenMustChangePasswordFalse_ReturnsTokensAsNormal`
  - Arrange: user with `MustChangePassword = false`, 2FA disabled
  - Assert: `result.Tokens != null`, `result.RequiresPasswordChange == false`

#### ForcedChangePasswordAsync tests

- `ForcedChangePasswordAsync_WithValidChallenge_UpdatesPasswordHash`
  - Assert: `user.PasswordHash` changed to hash of new password

- `ForcedChangePasswordAsync_WithValidChallenge_ClearsMustChangePasswordFlag`
  - Assert: `user.MustChangePassword == false`

- `ForcedChangePasswordAsync_WithValidChallenge_ReturnsTokens`
  - Assert: `result.AccessToken` is not empty

- `ForcedChangePasswordAsync_WithValidChallenge_MarksChalllengeAsUsed`
  - Assert: `challenge.IsUsed == true`

- `ForcedChangePasswordAsync_WithExpiredChallenge_ThrowsAuthException`
  - Arrange: `ExpiresAt = DateTime.UtcNow.AddMinutes(-1)`
  - Assert: throws `AuthException` with code `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE`

- `ForcedChangePasswordAsync_WithAlreadyUsedChallenge_ThrowsAuthException`
  - Arrange: `challenge.IsUsed = true`
  - Assert: throws `AuthException` with code `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE`

- `ForcedChangePasswordAsync_WithNonExistentChallengeId_ThrowsAuthException`
  - Arrange: random `Guid.NewGuid()` not in DB
  - Assert: throws `AuthException` with code `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE`

- `ForcedChangePasswordAsync_WhenUserIsInactive_ThrowsAuthException`
  - Arrange: valid challenge but `user.IsActive = false`
  - Assert: throws `AuthException` with code `AUTH_USER_INACTIVE`

---

### Step 18 — Integration tests

**File:** `tests/Auth.IntegrationTests/AuthControllerIntegrationTests.cs`

Add the following test cases. Use `IntegrationTestFixture` for real PostgreSQL.
Set `MustChangePassword = true` directly on the seeded user via the admin API
or by directly mutating the DB through the fixture's `DbContext`.

- `Login_WhenMustChangePassword_Returns200WithRequiresPasswordChangeTrue`
  - POST /api/auth/login with valid credentials for a user with the flag
  - Assert: 200, body has `requiresPasswordChange: true`

- `Login_WhenMustChangePassword_DoesNotReturnAccessToken`
  - Same setup
  - Assert: `tokens` field is null / absent

- `ForcedChangePassword_WithValidChallenge_Returns200WithTokens`
  - Obtain `passwordChangeChallengeId` from login response
  - POST /api/auth/password/forced-change
  - Assert: 200, body has `accessToken`

- `ForcedChangePassword_ThenLoginWithNewPassword_ReturnsTokens`
  - Full E2E: login (get challengeId) → forced-change → login with new password
  - Assert: final login returns tokens

- `ForcedChangePassword_WithExpiredChallenge_Returns401`
  - Insert a challenge with `ExpiresAt` in the past directly via DbContext
  - Assert: 401, code `AUTH_INVALID_PASSWORD_CHANGE_CHALLENGE`

- `ForcedChangePassword_WithUsedChallenge_Returns401`
  - Use the same challengeId twice
  - Assert: second call returns 401

- `ForcedChangePassword_WithRandomGuid_Returns401`
  - POST with `challengeId = Guid.NewGuid()`
  - Assert: 401

---

## Definition of Done

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] `POST /api/auth/login` response includes `requiresPasswordChange` and
      `passwordChangeChallengeId` fields (null when not applicable)
- [ ] `POST /api/auth/password/forced-change` issues real tokens
- [ ] Challenge cannot be reused or used after expiry
- [ ] `MustChangePassword` flag is cleared after successful change
- [ ] Flag can be set/cleared by admin via `PUT /api/users/{id}` and
      `PATCH /api/users/{id}`
- [ ] `UserDto` exposes `mustChangePassword`
- [ ] `docs/api.md` updated
- [ ] `docs/database.md` updated
- [ ] Migration applied cleanly on a fresh DB
