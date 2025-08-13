export interface ModelStatus {
  completed: boolean;
  percentage: number;
}

export interface FieldSpec {
  name: string;
  type: string;
}

const API_BASE = '/api/v1';

export async function switchModel(
  apiKey: string,
  params: { hfToken: string; hfRepo: string; modelFile: string; contextSize: number }
) {
  return fetch(`${API_BASE}/model/switch`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': apiKey,
    },
    body: JSON.stringify({
      hfKey: params.hfToken,
      modelRepo: params.hfRepo,
      modelFile: params.modelFile,
      contextSize: params.contextSize,
    }),
  });
}

export async function getModelStatus(apiKey: string): Promise<ModelStatus> {
  const res = await fetch(`${API_BASE}/model/status`, {
    headers: { 'X-API-Key': apiKey },
  });
  if (!res.ok) {
    throw new Error('Unable to fetch status');
  }
  return res.json();
}

export async function extractData(
  apiKey: string,
  params: { model: string; prompt: string; file: File; fields: FieldSpec[] },
) {
  const form = new FormData();
  form.append('model', params.model);
  form.append('prompt', params.prompt);
  form.append('fields', JSON.stringify(params.fields));
  form.append('file', params.file);

  const res = await fetch(`${API_BASE}/process`, {
    method: 'POST',
    headers: { 'X-API-Key': apiKey },
    body: form,
  });
  if (!res.ok) {
    throw new Error('Extraction failed');
  }
  return res.json();
}
