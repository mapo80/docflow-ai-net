import DataLoader from "@/components/DataLoader";

import React, { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Card, Descriptions, Tag, Space, Button, message, Tabs, Typography } from "antd";

const { Title } = Typography;

type AnyRec = Record<string, any>;

const JobDetail: React.FC = () => {
  const { id } = useParams();
  const [data, setData] = useState<AnyRec | null>(null);
  const [loading, setLoading] = useState(false);

  const load = async () => {
    if (!id) return;
    setLoading(true);
    try {
      const r = await fetch(`/api/v1/jobs/${id}`);
      const j = await r.json();
      setData(j);
    } catch (e: any) {
      message.error(e.message ?? "Load failed");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const status = data?.status ?? data?.Status;
  const progress = data?.progress ?? data?.Progress;
  const createdAt = data?.createdAt ?? data?.CreatedAt;
  const updatedAt = data?.updatedAt ?? data?.UpdatedAt;

  // Heuristics to surface template/model from various places
  const templateName = data?.templateName ?? data?.TemplateName ?? data?.meta?.templateName ?? data?.output?.templateName ?? "-";
  const modelName = data?.modelName ?? data?.ModelName ?? data?.meta?.modelName ?? data?.output?.modelName ?? "-";

  const markdown = data?.preview?.markdown ?? data?.output?.markdown ?? "";
  const fields = data?.preview?.fields ?? data?.output?.fields ?? data?.fields ?? null;

  if (loading || !data) { return <DataLoader />; }

  return (
    <div style={{ padding: 16 }}>
      <Space direction="vertical" size="large" style={{ width: "100%" }}>
        <Card title={<Space><Title level={4} style={{ margin: 0 }}>Job</Title><Tag color="blue">{id}</Tag></Space>} loading={loading}>
          <Descriptions bordered size="small" column={2} items={[
            { key: "status", label: "Status", children: <Tag color={status === "Succeeded" ? "green" : status === "Failed" ? "red" : "default"}>{String(status ?? "-")}</Tag> },
            { key: "progress", label: "Progress", children: `${progress ?? 0}%` },
            { key: "created", label: "Created", children: String(createdAt ?? "-") },
            { key: "updated", label: "Updated", children: String(updatedAt ?? "-") },
            { key: "template", label: "Template", children: String(templateName) },
            { key: "model", label: "Model", children: String(modelName) },
          ] as any} />
          <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 12 }}>
            <Button onClick={load}>Reload</Button>
          </div>
        </Card>

        <Card>
          <Tabs
            items={[
              { key: "markdown", label: "Markdown", children: <pre style={{ whiteSpace: "pre-wrap" }}>{markdown || "—"}</pre> },
              { key: "fields", label: "Fields (JSON)", children: <pre style={{ whiteSpace: "pre-wrap" }}>{fields ? JSON.stringify(fields, null, 2) : "—"}</pre> },
              { key: "raw", label: "Raw JSON", children: <pre style={{ whiteSpace: "pre-wrap", overflow: "auto" }}>{JSON.stringify(data ?? {}, null, 2)}</pre> },
            ]}
          />
        </Card>
      </Space>
    </div>
  );
};

export default JobDetail;
