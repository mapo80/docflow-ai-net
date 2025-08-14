export function openHangfire() {
  const base = import.meta.env.VITE_API_BASE_URL || '';
  const path = import.meta.env.VITE_HANGFIRE_PATH || '';
  const apiKey = localStorage.getItem('apiKey');
  const url = apiKey ? `${base}${path}?api_key=${encodeURIComponent(apiKey)}` : `${base}${path}`;
  window.open(url, '_blank', 'noopener,noreferrer');
}
