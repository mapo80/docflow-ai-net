export function openHangfire() {
  const base = import.meta.env.VITE_API_BASE_URL || window.location.origin;
  const path = import.meta.env.VITE_HANGFIRE_PATH || '/hangfire';
  const apiKey = localStorage.getItem('apiKey');
  const url = new URL(path, base);
  if (apiKey) url.searchParams.set('api_key', apiKey);
  window.open(url.toString(), '_blank', 'noopener,noreferrer');
}
