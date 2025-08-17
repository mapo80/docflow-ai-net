export interface RecognitionRunResponse {
  recognitionId: string;
  templateName: string;
  modelName: string;
  markdown: string;
  fieldsJson: string;
  createdAt: string;
}

const recognizeApi = {
  async run(file: File, modelName: string, templateName: string): Promise<RecognitionRunResponse> {
    const fd = new FormData();
    fd.append("file", file);
    fd.append("modelName", modelName);
    fd.append("templateName", templateName);
    const r = await fetch("/api/recognitions/run", { method: "POST", body: fd });
    if (!r.ok) throw new Error(await r.text());
    return await r.json();
  },
};

export default recognizeApi;
