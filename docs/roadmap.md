# Roadmap

Функционал, которого не хватает по сравнению с Keycloak. Registration flow не в скоупе — реализуется не в auth-сервисе.

## High Priority

### ~~Refresh Token Rotation~~ ✅
Реализовано через встроенный механизм OpenIddict (rolling refresh tokens). Новый refresh token выдаётся при каждом использовании, старый инвалидируется. Replay detection — при повторном использовании отозванного токена вся цепочка (authorization family) инвалидируется. Настраиваемый reuse leeway (`Integration__Oidc__RefreshTokenReuseLeewaySeconds`, default 30s).

### ~~Password Expiration~~ ✅
Per-user `PasswordMaxAgeDays` (nullable → fallback на глобальный `DefaultMaxAgeDays`, default 0 = отключено). При истечении — forced password change через существующий `PasswordChangeRequired` flow. Claim `pwd_exp` (unix timestamp) в access + id token для предупреждения на клиенте. Конфигурация: `Integration__PasswordExpiration__DefaultMaxAgeDays`.

### Session Management
- Хранение истории сессий (device, IP, user agent, время создания, последняя активность)
- Просмотр активных сессий пользователя (admin API + account API)
- Принудительное завершение сессии админом (revoke конкретной сессии)
- Realm-wide session revocation (разлогинить всех)
- Not-before policy — инвалидация всех токенов, выданных до определённого момента

### User Attributes
Произвольные key-value атрибуты на пользователях. Вопрос по формату значений:
- Простой вариант: `string -> string` (потребитель парсит JSON сам если нужно)
- Keycloak-вариант: `string -> List<string>`

Нужно определиться с форматом. Атрибуты доступны как claims в JWT — whitelist настраивается per-application (админ указывает какие атрибуты включать в токены конкретного клиента).

### TOTP (Authenticator App)
Поддержка TOTP через приложения (Google Authenticator, Authy, Microsoft Authenticator). Стандарт RFC 6238. В дополнение к существующему OTP по email/SMS. Recovery codes — резервные одноразовые коды на случай потери доступа к authenticator.

## Medium Priority

### SSO / Single Logout
- SSO-сессия между приложениями в рамках одного realm
- Back-Channel Logout (OIDC Back-Channel Logout spec) — сервер уведомляет приложения при logout
- Front-Channel Logout — browser-redirect logout

### Step-up Authentication
Запрос дополнительного фактора аутентификации для чувствительных операций. Приложение запрашивает повышенный уровень аутентификации (acr claim), сервис требует дополнительный фактор если текущая сессия его не прошла.

### Service Events
Механизм подписки на события для внешних систем:
- Authentication events (login, failed login, logout, MFA verify)
- Admin events (user created, role changed, etc.)
- Варианты доставки: webhooks, message queue (RabbitMQ/Kafka), или polling API
- Интеграция с SIEM, мониторингом, аналитикой

### Clustering / HA
Горизонтальное масштабирование:
- Распределённый кеш сессий (Redis)
- Синхронизация token revocation между инстансами
- Health check с учётом кластера
- Stateless-архитектура (JWT уже помогает, но refresh tokens и OTP challenges требуют shared state)

## Low Priority

### Device Authorization Grant (RFC 8628)
Авторизация на устройствах без клавиатуры (Smart TV, CLI tools, IoT). Device получает device code + user code, пользователь вводит код на другом устройстве.

### Token Exchange (RFC 8693)
Обмен токенов между клиентами:
- Impersonation — привилегированный клиент получает токен от имени пользователя
- Delegation — сервис обменивает свой токен на токен для downstream-сервиса
- Federation token swap — обмен внешнего токена на внутренний

---

## Excluded

- **Registration flow** — реализуется не в auth-сервисе
- **SAML 2.0** — нет текущей потребности
- **Groups / Composite Roles** — покрывается комбинацией Role + Domain + Workspace
- **Admin Console** — уже реализована
- **Brute Force Protection** — TODO: уточнить, есть ли уже или нужно добавить
- **Password History** — TODO: уточнить приоритет
- **WebAuthn / Passkeys** — TODO: уточнить приоритет
- **UMA 2.0 / Resource-based permissions** — избыточно при наличии bitmask RBAC
- **FAPI / DPoP / PAR** — нет текущей потребности в financial-grade compliance
