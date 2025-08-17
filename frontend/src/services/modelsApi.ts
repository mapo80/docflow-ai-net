export type ModelSourceType = "Local" | "Url" | "HuggingFace" | "OpenAI" | "AzureOpenAI" | "OpenAICompatible";
export type ModelStatus = "NotDownloaded" | "Downloading" | "Available" | "Error" | "Deleting";

export interface ModelDto {
  id: string;
  name: string;
  sourceType: ModelSourceType;
  localPath?: string | null;
  url?: string | null;
  hfRepo?: string | null;
  hfRevision?: string | null;
  hfFilename?: string | null;
  endpoint?: string | null;
  apiKey?: string | null;
  model?: string | null;
  organization?: string | null;
  apiVersion?: string | null;
  deployment?: string | null;
  extraHeadersJson?: string | null;
  sha256?: string | null;
  fileSize?: number | null;
  status: ModelStatus;
  downloadProgress: number;
  createdAt: string;
  lastUsedAt?: string | null;
  isActive: boolean;
  errorMessage?: string | null;
}

export interface AddModelRequest {
  name: string;
  sourceType: ModelSourceType;
  localPath?: string | null;
  url?: string | null;
  hfRepo?: string | null;
  hfRevision?: string | null;
  hfFilename?: string | null;
  sha256?: string | null;
  endpoint?: string | null;
  apiKey?: string | null;
  model?: string | null;
  organization?: string | null;
  apiVersion?: string | null;
  deployment?: string | null;
  extraHeadersJson?: string | null;
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
  async download(id: string): Promise<void> {
    const r = await fetch(`/api/models/${id}/download`, { method: "POST" });
    if (!r.ok) throw new Error(await r.text());
  },
  async activate(id: string): Promise<void> {
    const r = await fetch(`/api/models/${id}/activate`, { method: "POST" });
    if (!r.ok) throw new Error(await r.text());
  },
  async remove(id: string): Promise<void> {
    const r = await fetch(`/api/models/${id}`, { method: "DELETE" });
    if (!r.ok) throw new Error(await r.text());
  },
};

export default api;
