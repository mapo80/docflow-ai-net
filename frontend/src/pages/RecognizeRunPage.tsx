import DataLoader from "@/components/DataLoader";
import React, { useEffect, useState } from "react";
import { Card, Form, Input, Upload, Button, Select, message } from "antd";
import recognizeApi from "@/services/recognizeApi";
import templatesApi, { TemplateDto } from "@/services/templatesApi";
import modelsApi, { ModelDto } from "@/services/modelsApi";

const RecognizeRunPage: React.FC = () => {
  const [file, setFile] = useState<File | null>(null);
  const [templates, setTemplates] = useState<TemplateDto[]>([]);
  const [models, setModels] = useState<ModelDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [md, setMd] = useState("");
  const [fields, setFields] = useState("");

  useEffect(() => {
    templatesApi.list().then(setTemplates);
    modelsApi.list().then(setModels);
  }, []);

  const run = async (values: any) => {
    if (!file) { message.error("Select a file"); return; }
    setLoading(true);
    try {
      const res = await recognizeApi.run(file, values.modelName, values.templateName);
      setMd(res.markdown);
      setFields(res.fieldsJson);
      message.success("Recognition done");
    } catch (e: any) {
      message.error(e.message ?? "Run failed");
    } finally {
      setLoading(false);
    }
  };

  if (loading) { return <DataLoader />; }

  return (
    <div style={{ padding: 16, display: "grid", gap: 16 }}>
      <Card title="Run Recognition with Template">
        <Form layout="vertical" onFinish={run}>
          <Form.Item name="templateName" label="Template" rules={[{ required: true }]}>
            <Select options={templates.map(t => ({ value: t.name, label: t.name }))} />
          </Form.Item>
          <Form.Item name="modelName" label="Model" rules={[{ required: true }]}>
            <Select options={models.map(m => ({ value: m.name, label: m.name }))} />
          </Form.Item>
          <Form.Item label="File" required>
            <Upload beforeUpload={(f) => { setFile(f); return false; }} maxCount={1}>
              <Button>Select file</Button>
            </Upload>
          </Form.Item>
          <Button type="primary" htmlType="submit" loading={loading}>Run</Button>
        </Form>
      </Card>

      {md && (
        <Card title="Markdown">
          <pre style={{ whiteSpace: "pre-wrap" }}>{md}</pre>
        </Card>
      )}
      {fields && (
        <Card title="Fields (JSON)">
          <pre style={{ whiteSpace: "pre-wrap" }}>{fields}</pre>
        </Card>
      )}
    </div>
  );
};

export default RecognizeRunPage;
