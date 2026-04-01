# TODO

## P0 — Critical

### 1. Rate Limiting на login, 2FA, password reset
**Проблема:** Ни один endpoint не защищён от brute-force. Атакующий может неограниченно подбирать пароли, OTP-коды, создавать challenge.

**Решение:** На уровне API Gateway (nginx/Envoy/etc). Минимум:
- Login: 5 попыток / 15 мин на IP + username
- 2FA challenge creation: 3 / час на пользователя
- Password reset: 3 / час на email

---

### 2. Account Lockout
**Проблема:** `User.cs` не отслеживает неудачные попытки входа. Даже при rate limiting на gateway (по IP), распределённый подбор паролей с разных IP не блокируется.

**Решение:** Добавить поля в `User` entity:
- `FailedLoginAttempts` (int)
- `LockedOutUntil` (DateTime?)

В `ValidateCredentialsCommandHandler`: проверять lockout перед верификацией пароля, инкрементировать при неудаче, сбрасывать при успехе. Например: 5 попыток → блокировка 15 мин.

---

### 3. Re-authentication для отключения 2FA
**Проблема:** Эндпоинт `POST /api/account/2fa/disable` позволял отключить 2FA имея только валидный JWT, без подтверждения пароля или OTP. Эндпоинт временно убран из AccountController.

**Решение:** Реализовать заново с обязательной re-authentication (пароль и/или текущий OTP) перед отключением. Команда `DisableTwoFactorCommand` и хендлер сохранены.

---

### 4. Recovery codes для 2FA
**Проблема:** При потере доступа к email/телефону (канал 2FA) пользователь не может самостоятельно восстановить доступ к аккаунту — только через админа.

**Решение:** При включении 2FA генерировать набор (8-10) одноразовых recovery codes. Пользователь сохраняет их. При входе можно использовать recovery code вместо OTP.

---

## Инфраструктура

### 5. Аутентификация тестового SMS-шлюза
Тестовый SMS-шлюз хранит OTP в Redis и отдаёт по URL без аутентификации. Добавить API-ключ или basic auth для доступа к эндпоинту чтения SMS в dev-окружении.
