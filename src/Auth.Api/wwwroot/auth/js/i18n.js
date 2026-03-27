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

    // verify
    verifyEmailTitle: 'Подтверждение email',
    verifyPhoneTitle: 'Подтверждение телефона',
    verifyLoading: 'Подтверждение...',
    verifySuccess: 'Подтверждено',
    verifyEmailSuccess: 'Ваш email успешно подтверждён',
    verifyPhoneSuccess: 'Ваш телефон успешно подтверждён',
    verifyFailed: 'Ошибка подтверждения',
    verifyFailedHint: 'Код недействителен или срок действия истёк. Запросите новый код.',
    verifyInvalidLink: 'Недействительная ссылка',
    verifyInvalidLinkHint: 'Ссылка повреждена или неполная. Запросите новую ссылку для подтверждения.',

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
