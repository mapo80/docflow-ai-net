export interface TemplateDto {
  id: string;
  name: string;
  documentType: string;
  language: string;
  fieldsJson: string;
  notes?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface TemplateUpsertRequest {
  name: string;
  documentType?: string | null;
  language?: string | null;
  fieldsJson?: string | null;
  notes?: string | null;
}

const api = {
  async list(): Promise<TemplateDto[]> {
    const r = await fetch("/api/templates");
    if (!r.ok) throw new Error(await r.text());
    return await r.json();
  },
  async create(req: TemplateUpsertRequest): Promise<TemplateDto> {
    const r = await fetch("/api/templates", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });
    if (!r.ok) throw new Error(await r.text());
    return await r.json();
  },
  async update(id: string, req: TemplateUpsertRequest): Promise<TemplateDto> {
    const r = await fetch(`/api/templates/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(req),
    });
    if (!r.ok) throw new Error(await r.text());
    return await r.json();
  },
  async remove(id: string): Promise<void> {
    const r = await fetch(`/api/templates/${id}`, { method: "DELETE" });
    if (!r.ok) throw new Error(await r.text());
  },
};

export default api;
