export interface ErrorResponse {
  errorCode: string;
  message?: string;
  retry_after_seconds?: number;
}

export class HttpError extends Error {
  status: number;
  data: ErrorResponse;
  retryAfter?: number;

  constructor(status: number, data: ErrorResponse, retryAfter?: number) {
    super(data.errorCode);
    this.status = status;
    this.data = data;
    this.retryAfter = retryAfter;
  }
}

const base = import.meta.env.VITE_API_BASE_URL || '';
const normalized = base.replace(/\/$/, '');
const BASE_URL = normalized.endsWith('/api/v1') ? normalized : `${normalized}/api/v1`;

export async function fetcher(input: string, init: RequestInit): Promise<Response> {
  const url = input.startsWith('http') ? input : `${BASE_URL}${input}`;
  const res = await fetch(url, init);
  if (res.ok) {
    return res;
  }
  let retryAfter: number | undefined = undefined;
  const retryHeader = res.headers.get('Retry-After');
  if (retryHeader) {
    const n = Number.parseInt(retryHeader, 10);
    if (!Number.isNaN(n)) retryAfter = n;
  }
  let data: ErrorResponse = { errorCode: res.statusText };
  try {
    const body = await res.clone().json();
    data = body;
    if (body.retry_after_seconds && !retryAfter) {
      retryAfter = body.retry_after_seconds;
    }
  } catch {
    // ignore
  }
  throw new HttpError(res.status, data, retryAfter);
}
