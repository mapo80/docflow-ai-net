
import React, { useEffect, useMemo, useState } from "react";
import { Card, Input, Upload, Button, Tabs, Select, Space, message, Typography, Divider, Switch } from "antd";
import { InboxOutlined } from "@ant-design/icons";
import MDEditor from "@uiw/react-md-editor";
import modelsApi, { type ModelDto } from "@/services/modelsApi";
import templatesApi, { type TemplateDto } from "@/services/templatesApi";
import { useNavigate } from "react-router-dom";

const { Title, Text } = Typography;
const { Dragger } = Upload;

type Mode = "saved" | "inline";

const JobNew: React.FC = () => {
  const navigate = useNavigate();

  const [file, setFile] = useState<File | null>(null);
  const [prompt, setPrompt] = useState<string>("");
  const [mode, setMode] = useState<Mode>("saved");
  const [selectedTemplateName, setSelectedTemplateName] = useState<string | undefined>(undefined);
  const [inlineTemplate, setInlineTemplate] = useState<string>("[]");
  const [models, setModels] = useState<ModelDto[]>([]);
  const [templates, setTemplates] = useState<TemplateDto[]>([]);
  const [selectedModelName, setSelectedModelName] = useState<string | undefined>(undefined);
  const [immediate, setImmediate] = useState<boolean>(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    modelsApi.list().then((m) => {
      setModels(m);
      const active = m.find((x) => x.downloaded);
      if (active) setSelectedModelName(active.name);
    }).catch(() => {});
    templatesApi.list().then(setTemplates).catch(() => {});
  }, []);

  const modelOptions = useMemo(() => models.map(m => ({
    value: m.name,
    label: `${m.name} (${m.type})`,
    disabled: !m.downloaded
  })), [models]);

  const templateOptions = useMemo(() => templates.map(t => ({
    value: t.name,
    label: `${t.name} • ${t.documentType} • ${t.language}`
  })), [templates]);

  const fieldsJson = useMemo(() => {
    if (mode === "saved") {
      const tpl = templates.find(t => t.name === selectedTemplateName);
      return tpl?.fieldsJson ?? "[]";
    }
    return inlineTemplate;
  }, [mode, selectedTemplateName, templates, inlineTemplate]);

  const onSubmit = async () => {
    if (!file) { message.error("Select a file"); return; }
    if (!selectedModelName) { message.error("Select a model"); return; }
    if (mode === "saved" && !selectedTemplateName) { message.error("Select a template"); return; }
    try {
      JSON.parse(fieldsJson);
    } catch {
      message.error("Invalid fields JSON"); return;
    }

    setSubmitting(true);
    try {
      const fd = new FormData();
      fd.append("file", file);
      fd.append("prompt", prompt ?? "");
      fd.append("fields", fieldsJson);
      fd.append("modelName", selectedModelName);
      if (mode === "saved" && selectedTemplateName) {
        fd.append("templateName", selectedTemplateName);
      }
      if (immediate) fd.append("immediate", "true");

      // Submit to existing Jobs API (multipart). Fallback to /api/recognitions/run if immediate desired.
      const endpoint = "/api/v1/jobs";
      const res = await fetch(endpoint, { method: "POST", body: fd });
      if (!res.ok) {
        const txt = await res.text();
        throw new Error(txt || "Submit failed");
      }
      const data = await res.json().catch(() => ({} as any));
        // try to navigate using returned id
        const id = data?.id ?? data?.jobId ?? data?.JobId;
        if (id) {
          message.success("Job created");
          navigate(`/jobs/${id}`);
        } else {
          message.success("Submitted");
        }
      } catch (e: any) {
        message.error(e.message ?? "Submit error");
      } finally {
        setSubmitting(false);
      }
    };

  return (
    <div style={{ padding: 16 }}>
      <Card title="New Job" style={{ maxWidth: 1200, margin: "0 auto" }}>
        <Space direction="vertical" size="large" style={{ width: "100%" }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
              <Card size="small" bordered>
                <Title level={5} style={{ marginBottom: 12 }}>Document</Title>
                <Dragger
                name="file"
                multiple={false}
                beforeUpload={(f) => { setFile(f); return false; }}
                maxCount={1}
              >
                <p className="ant-upload-drag-icon">
                  <InboxOutlined />
                </p>
                <p className="ant-upload-text">Drag file or click to select</p>
                {file && <Text type="secondary">Selected: {file.name}</Text>}
              </Dragger>
            </Card>

              <Card size="small" bordered>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <Title level={5} style={{ marginBottom: 12 }}>Model</Title>
                <Text type="secondary">Only "Available" models</Text>
              </div>
              <Select
                style={{ width: "100%" }}
                placeholder="Select model"
                options={modelOptions}
                value={selectedModelName}
                onChange={setSelectedModelName}
                showSearch
                optionFilterProp="label"
              />
              <div style={{ marginTop: 12 }}>
                <Space>
                  <Text>Immediate</Text>
                  <Switch checked={immediate} onChange={setImmediate} />
                </Space>
              </div>
            </Card>
          </div>

            <Card size="small" bordered>
            <Title level={5} style={{ marginBottom: 12 }}>Template</Title>
              <Tabs activeKey={mode} onChange={(k) => setMode(k as Mode)} items={[
              {
                key: "saved",
                label: "Select saved template",
                children: (
                  <Select
                    style={{ width: "100%" }}
                    placeholder="Select template"
                    options={templateOptions}
                    value={selectedTemplateName}
                    onChange={setSelectedTemplateName}
                    showSearch
                    optionFilterProp="label"
                  />
                )
              },
              {
                key: "inline",
                label: "Paste template (JSON)",
                children: (
                  <Input.TextArea
                    value={inlineTemplate}
                    onChange={(e) => setInlineTemplate(e.target.value)}
                    autoSize={{ minRows: 8 }}
                    placeholder='[{"key":"total","description":"...","type":"number","required":true}]'
                  />
                )
              }
            ]} />
              <Divider orientation="left">Fields preview</Divider>
              <pre style={{ background: "#fafafa", padding: 12, borderRadius: 8, maxHeight: 240, overflow: "auto" }}>{fieldsJson}</pre>
            </Card>

            <Card size="small" bordered>
              <Title level={5} style={{ marginBottom: 12 }}>Prompt (optional)</Title>
              <div data-color-mode="light">
                <MDEditor value={prompt} onChange={(v) => setPrompt(v || "")} height={200} />
              </div>
            </Card>

            <div style={{ display: "flex", justifyContent: "flex-end", gap: 12 }}>
              <Button onClick={() => navigate(-1)}>Cancel</Button>
              <Button type="primary" onClick={onSubmit} loading={submitting} disabled={!file}>Create Job</Button>
            </div>
          </Space>
        </Card>
      </div>
  );
}; 

export default JobNew;

export const isValidJson = (value: string): boolean => {
  try {
    JSON.parse(value);
    return true;
  } catch {
    return false;
  }
};

export const validateFile = (file: File): string | undefined => {
  const allowed = [".pdf", ".png", ".jpg", ".jpeg"];
  const ext = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
  if (!allowed.includes(ext)) return "Invalid extension";
  if (file.size > 10 * 1024 * 1024) return "File too large";
  return undefined;
};

export const buildPayload = async (
  file: File,
  prompt: string,
  fieldsJson: string
): Promise<FormData> => {
  const fd = new FormData();
  fd.append("file", file);
  fd.append("prompt", prompt);
  fd.append("fields", fieldsJson);
  return fd;
};

export const submitPayload = async (
  payload: FormData,
  immediate: boolean
) => {
  const url = immediate ? "/api/v1/jobs?mode=immediate" : "/api/v1/jobs";
  const res = await fetch(url, { method: "POST", body: payload });
  if (!res.ok) {
    const txt = await res.text();
    throw new Error(txt || "Submit failed");
  }
  return res.json();
};
