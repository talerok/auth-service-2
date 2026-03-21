import { t } from './i18n.js';

export async function api(url, method = 'GET', body) {
  try {
    const options = { method, headers: {} };
    if (body !== undefined) {
      options.headers['Content-Type'] = 'application/json';
      options.body = JSON.stringify(body);
    }
    const res = await fetch(url, options);
    if (res.status === 204) return { ok: true, data: {} };
    const data = await res.json().catch(() => ({}));
    return { ok: res.ok, data };
  } catch {
    return { ok: false, data: {}, error: t('connectionError') };
  }
}

export function getParams(...keys) {
  const params = new URLSearchParams(location.search);
  if (keys.length === 1) return params.get(keys[0]);
  return Object.fromEntries(keys.map(k => [k, params.get(k)]));
}
