export type ModelType = "hosted-llm" | "local";

export type HostedProvider = "openai" | "azure-openai";

export type DownloadState =
  | "pending"
  | "downloading"
  | "downloaded"
  | "failed"
  | "canceled";

export interface ModelDto {
  id: string;
  name: string;
  type: ModelType;
  provider?: HostedProvider | null;
  baseUrl?: string | null;
  hfRepo?: string | null;
  modelFile?: string | null;
  downloaded: boolean | null;
  downloadStatus?: DownloadState | null;
}

export interface AddModelRequest {
  name: string;
  type: ModelType;
  provider?: HostedProvider | null;
  baseUrl?: string | null;
  apiKey?: string | null;
  hfToken?: string | null;
  hfRepo?: string | null;
  modelFile?: string | null;
  downloadNow?: boolean;
}

const api = {
  async list(): Promise<ModelDto[]> {
    const r = await fetch("/api/models");
    if (!r.ok) throw new Error("Failed to list models");
    return await r.json();
  },
  async add(req: AddModelRequest): Promise<ModelDto> {
    const r = await fetch("/api/models", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });
    if (!r.ok) throw new Error(await r.text());
    return await r.json();
  },
  async update(id: string, req: AddModelRequest): Promise<ModelDto> {
    const r = await fetch(`/api/models/${id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });
    if (!r.ok) throw new Error(await r.text());
    return await r.json();
  },
  async remove(id: string, deleteLocalFile?: boolean): Promise<void> {
    const r = await fetch(`/api/models/${id}?deleteLocalFile=${deleteLocalFile ? "true" : "false"}`, {
      method: "DELETE",
    });
    if (!r.ok) throw new Error(await r.text());
  },
  async startDownload(id: string): Promise<void> {
    const r = await fetch(`/api/models/${id}/download`, { method: "POST" });
    if (!r.ok) throw new Error(await r.text());
  },
  async cancelDownload(id: string): Promise<void> {
    const r = await fetch(`/api/models/${id}/cancel-download`, { method: "POST" });
    if (!r.ok) throw new Error(await r.text());
  },
  async status(id: string): Promise<DownloadState> {
    const r = await fetch(`/api/models/${id}/download-status`);
    if (!r.ok) throw new Error(await r.text());
    const json = await r.json();
    return json.status as DownloadState;
  },
  async testConnection(provider: HostedProvider, baseUrl: string, apiKey: string): Promise<void> {
    const r = await fetch(`/api/models/test-connection`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ provider, baseUrl, apiKey }),
    });
    if (!r.ok) throw new Error(await r.text());
  },
};

export default api;
