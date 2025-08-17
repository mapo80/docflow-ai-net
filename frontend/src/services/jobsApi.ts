
export type JobsListParams = {
  page?: number;
  pageSize?: number;
  q?: string;
  status?: string[];
  from?: string; // ISO
  to?: string;   // ISO
  immediate?: boolean;
};

export async function listJobs(params: JobsListParams) {
  const url = new URL('/api/v1/jobs', window.location.origin);
  if (params.page) url.searchParams.set('page', String(params.page));
  if (params.pageSize) url.searchParams.set('pageSize', String(params.pageSize));
  if (params.q) url.searchParams.set('q', params.q);
  if (params.status && params.status.length) {
    for (const s of params.status) url.searchParams.append('status', s);
  }
  if (params.from) url.searchParams.set('from', params.from);
  if (params.to) url.searchParams.set('to', params.to);
  if (typeof params.immediate === 'boolean') url.searchParams.set('immediate', String(params.immediate));
  const r = await fetch(url.toString());
  if (!r.ok) throw new Error(await r.text());
  return await r.json();
}

export async function cancelJob(id: string) {
  const r = await fetch(`/api/v1/jobs/${id}`, { method: 'DELETE' });
  if (!r.ok) throw new Error(await r.text());
  return true;
}
