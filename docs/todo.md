# TODO

## P0 — Critical

### 1. Rate Limiting на login, 2FA, password reset

**Проблема:** Ни один endpoint не защищён от brute-force. Атакующий может неограниченно подбирать пароли, OTP-коды, создавать challenge.

**Решение:** На уровне API Gateway (nginx/Envoy/etc). Минимум:

- Login: 5 попыток / 15 мин на IP + username
- 2FA challenge creation: 3 / час на пользователя
- Password reset: 3 / час на email

---

### ~~2. Account Lockout~~ ✅

Реализовано. `User.FailedLoginAttempts` + `LockoutEndTime`, конфиг `AccountLockoutOptions` (MaxFailedAttempts=5, LockoutDurationMinutes=15). Проверка в `ValidateCredentialsCommandHandler` и `TouchSessionCommandHandler`.

---

### 3. Re-authentication для отключения 2FA

**Проблема:** Эндпоинт `POST /api/account/2fa/disable` позволял отключить 2FA имея только валидный JWT, без подтверждения пароля или OTP. Эндпоинт временно убран из AccountController.

**Решение:** Реализовать заново с обязательной re-authentication (пароль и/или текущий OTP) перед отключением. Команда `DisableTwoFactorCommand` и хендлер сохранены.

---

### 4. Recovery codes для 2FA

**Проблема:** При потере доступа к email/телефону (канал 2FA) пользователь не может самостоятельно восстановить доступ к аккаунту — только через админа.

**Решение:** При включении 2FA генерировать набор (8-10) одноразовых recovery codes. Пользователь сохраняет их. При входе можно использовать recovery code вместо OTP.
