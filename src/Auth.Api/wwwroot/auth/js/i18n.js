const lang = document.documentElement.lang || 'ru';

const messages = {
  ru: {
    // common
    connectionError: 'Ошибка соединения',

    // login
    loginTitle: 'Вход',
    username: 'Имя пользователя',
    password: 'Пароль',
    loginSubmit: 'Войти',
    loginLoading: 'Вход...',
    invalidCredentials: 'Неверные учётные данные',

    // mfa
    mfaTitle: 'Подтверждение',
    mfaCodeSentTo: 'Код отправлен на',
    mfaChannelEmail: 'email',
    mfaChannelSms: 'телефон',
    mfaCode: 'Код подтверждения',
    mfaSubmit: 'Подтвердить',
    mfaLoading: 'Проверка...',
    mfaInvalidCode: 'Неверный код',

    // consent
    consentTitle: 'Доступ',
    consentRequestsAccess: 'запрашивает доступ',
    consentDeny: 'Отклонить',
    consentAllow: 'Разрешить',
    scopeOpenid: 'Идентификация',
    scopeProfile: 'Профиль (имя)',
    scopeEmail: 'Email',
    scopePhone: 'Телефон',
    scopeWs: 'Рабочие пространства',
    scopeOfflineAccess: 'Offline-доступ (refresh token)',

    // password change
    passwordChangeTitle: 'Смена пароля',
    newPassword: 'Новый пароль',
    confirmPassword: 'Подтверждение пароля',
    minLength: 'Минимум ${n} символов',
    requireUppercase: 'Заглавная буква',
    requireLowercase: 'Строчная буква',
    requireDigit: 'Цифра',
    requireSpecialCharacter: 'Спецсимвол',
    passwordsMatch: 'Пароли совпадают',
    passwordChangeSubmit: 'Сменить пароль',
    passwordChangeLoading: 'Сохранение...',
    passwordChangeFailed: 'Не удалось сменить пароль',
  }
};

export function t(key, params) {
  let text = messages[lang]?.[key] ?? messages['ru']?.[key] ?? key;
  if (params) {
    for (const [k, v] of Object.entries(params)) {
      text = text.replace('${' + k + '}', v);
    }
  }
  return text;
}
