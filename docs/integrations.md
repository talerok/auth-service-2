# Integrations

Все интеграции конфигурируются через секцию `Integration` в `appsettings` / env-переменные `Integration__*`.

---

## PostgreSQL

```
Integration__PostgreSql__ConnectionString=Host=postgres;Port=5432;Database=auth;Username=postgres;Password=postgres
```

Основная БД сервиса. EF Core + Npgsql. Миграции применяются автоматически при старте.

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
