# Integrations

Все интеграции конфигурируются через секцию `Integration` в `appsettings` / env-переменные `Integration__*`.

---

## PostgreSQL

```
Integration__PostgreSql__ConnectionString=Host=postgres;Port=5432;Database=auth;Username=postgres;Password=postgres
```

Основная БД сервиса. EF Core + Npgsql. Миграции применяются автоматически при старте.

---

## Encryption Key

```
Integration__EncryptionKey=<min 32 chars>
```

Ключ шифрования секретов at-rest (OIDC client secrets, LDAP bind passwords). **Обязателен** — сервис не стартует без него.

---

## OIDC / OpenIddict

```
Integration__Oidc__AccessTokenLifetimeMinutes=15
Integration__Oidc__RefreshTokenLifetimeDays=7
Integration__Oidc__RefreshTokenReuseLeewaySeconds=30
Integration__Oidc__LoginUrl=/auth/login.html
Integration__Oidc__ConsentUrl=/auth/consent.html
Integration__Oidc__SigningKeyPath=
Integration__Oidc__SigningKeyPassword=
Integration__Oidc__EncryptionKeyPath=
Integration__Oidc__EncryptionKeyPassword=
```

Параметры OpenIddict. В production требуются X.509-сертификаты для подписи и шифрования токенов (`SigningKeyPath`, `EncryptionKeyPath`). В Development/Testing используются dev-сертификаты.

**Refresh token rotation** — при обмене refresh token выдаётся новый. `RefreshTokenReuseLeewaySeconds` (default 30) — окно допустимого повторного использования redeemed-токена (для race conditions в распределённых системах). При replay за пределами окна вся token family отзывается.

---

## SMTP (Email 2FA)

```
Integration__Smtp__Enabled=true
Integration__Smtp__Host=mailhog
Integration__Smtp__Port=1025
Integration__Smtp__UseSsl=false
Integration__Smtp__Username=
Integration__Smtp__Password=
Integration__Smtp__FromEmail=noreply@auth-service
Integration__Smtp__FromName=Auth Service
```

Доставка OTP-кодов по email. При `Enabled=false` используется `SafeDefaultTwoFactorEmailGateway` (в dev/testing — автоматический `Delivered`).

---

## SMS Gateway (SMS 2FA)

```
Integration__SmsGateway__Enabled=true
Integration__SmsGateway__BaseUrl=http://sms-gateway:8080
Integration__SmsGateway__ApiKey=<secret>
Integration__SmsGateway__TimeoutSeconds=5
```

HTTP-клиент к промежуточному SMS-сервису. Аутентификация по `X-Api-Key`. При `Enabled=false` используется `SafeDefaultTwoFactorSmsGateway`.

Auth-сервис не зависит от конкретного SMS-провайдера — между ними промежуточный сервис (SMS Gateway).

```
Auth Backend  ──HTTP──▸  SMS Gateway  ──▸  SMS Provider
```

### `POST /api/sms/send`

Request:

```json
{
  "requestId": "<challengeId, UUID, идемпотентный ключ>",
  "phone": "+71234567890",
  "message": "Your code: 482916"
}
```

Responses:

| HTTP    | Body                                         | → `TwoFactorDeliveryResult` |
| ------- | -------------------------------------------- | --------------------------- |
| `200`   | `{"status":"accepted"}`                      | `Delivered`                 |
| `422`   | `{"status":"rejected","reason":"<причина>"}` | `DeliveryFailed`            |
| `503`   | `{"status":"unavailable"}`                   | `ProviderUnavailable`       |
| timeout | —                                            | `ProviderUnavailable`       |

Причины `422`: `invalid_phone`, `phone_blocked`, `rate_limited`, `message_too_long`.

---

## Verification URLs

```
Integration__Verification__EmailBaseUrl=http://localhost:4000/auth/verify.html?type=email
Integration__Verification__PhoneBaseUrl=http://localhost:4000/auth/verify.html?type=phone
```

Базовые URL для ссылок верификации email/телефона. Подставляются в шаблоны уведомлений как `{{link}}` с добавлением `&challengeId=...&otp=...`.

---

## CORS

```
Integration__Cors__AllowedOrigins=http://localhost:4200
```

Список допустимых origins через запятую. Применяется к OIDC-эндпоинтам (`/connect/*`).

---

## OpenSearch

```
Integration__OpenSearch__Url=http://opensearch:9200
Integration__OpenSearch__IndexPrefix=auth
Integration__OpenSearch__EnsureIndicesOnStartup=true
Integration__OpenSearch__ReindexOnStartup=false
Integration__OpenSearch__Username=
Integration__OpenSearch__Password=
```

Full-text search по пользователям, ролям, разрешениям, workspace-ам. Подключается через `COMPOSE_PROFILES=local-opensearch`. При отсутствии URL используется `NullSearchIndexService` (no-op).

---

## Password Requirements

```
Integration__PasswordRequirements__MinLength=8
Integration__PasswordRequirements__MaxLength=128
Integration__PasswordRequirements__RequireUppercase=true
Integration__PasswordRequirements__RequireLowercase=true
Integration__PasswordRequirements__RequireDigit=true
Integration__PasswordRequirements__RequireSpecialCharacter=false
```

Парольная политика. Отдаётся клиенту через `GET /api/account/password-requirements`.

---

## Password Expiration

```
Integration__PasswordExpiration__DefaultMaxAgeDays=0
```

Глобальный срок действия пароля (в днях). `0` = отключено. Может быть переопределён per-user через `password_max_age_days`. При истечении пароля в токен добавляется claim `pwd_exp` (unix timestamp).

---

## 2FA Delivery (Tuning)

```
Integration__TwoFactor__OtpLength=6
Integration__TwoFactor__StandardOtpTtlMinutes=5
Integration__TwoFactor__HighRiskOtpTtlMinutes=3
Integration__TwoFactor__MaxAttemptsPerChallenge=5
Integration__TwoFactor__DeliveryTimeoutSeconds=3
Integration__TwoFactor__DeliveryRetryCount=3
Integration__TwoFactor__DeliveryRetryBackoffMilliseconds=200
Integration__TwoFactor__DeliveryPollIntervalMilliseconds=2000
Integration__TwoFactor__StaticOtpForTesting=
```

Параметры фоновой доставки OTP. `StaticOtpForTesting` — фиксированный OTP для тестов (только в Development/Testing).

---

## MailHog (dev)

Docker-compose профиль `mailhog` запускает MailHog — mock SMTP-сервер с веб-интерфейсом.

```bash
COMPOSE_PROFILES=mailhog docker compose up -d mailhog
```

- SMTP: `localhost:1025`
- Web UI: `http://localhost:8025`
