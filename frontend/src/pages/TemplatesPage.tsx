import DataLoader from "@/components/DataLoader";
import React, { useEffect, useMemo, useState } from "react";
import { Card, Button, Table, Tag, message, Popconfirm } from "antd";
import TemplateModal from "@/components/templates/TemplateModal";
import api, { type TemplateDto, type TemplateUpsertRequest } from "@/services/templatesApi";

const TemplatesPage: React.FC = () => {
  const [data, setData] = useState<TemplateDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [edit, setEdit] = useState<TemplateDto | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      setData(await api.list());
    } catch (e: any) {
      message.error(e.message ?? "Failed to load templates");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const onCreate = async (req: TemplateUpsertRequest) => {
    try {
      await api.create(req);
      message.success("Template created");
      load();
    } catch (e: any) {
      message.error(e.message ?? "Create failed");
    }
  };

  const onUpdate = async (req: TemplateUpsertRequest) => {
    if (!edit) return;
    try {
      await api.update(edit.id, req);
      message.success("Template updated");
      setEdit(null);
      load();
    } catch (e: any) {
      message.error(e.message ?? "Update failed");
    }
  };

  const onDelete = async (id: string) => {
    try {
      await api.remove(id);
      message.success("Template deleted");
      load();
    } catch (e: any) {
      message.error(e.message ?? "Delete failed");
    }
  };

  const columns = useMemo(() => [
    { title: "Name", dataIndex: "name", key: "name" },
    { title: "Type", dataIndex: "documentType", key: "documentType", render: (v: string) => <Tag>{v}</Tag> },
    { title: "Language", dataIndex: "language", key: "language", render: (v: string) => <Tag>{v}</Tag> },
    { title: "Fields", key: "fields", render: (_: any, r: TemplateDto) => {
      try { const arr = JSON.parse(r.fieldsJson); return <span>{Array.isArray(arr) ? arr.length : 0}</span>; } catch { return <span>0</span>; }
    }},
    { title: "Actions", key: "actions", render: (_: any, r: TemplateDto) => (
      <div style={{ display: "flex", gap: 8 }}>
        <Button onClick={() => setEdit(r)}>Edit</Button>
        <Popconfirm title="Delete template?" onConfirm={() => onDelete(r.id)}><Button danger>Delete</Button></Popconfirm>
      </div>
    )},
  ], [edit]);

  if (loading) { return <DataLoader />; }

  return (
    <div style={{ padding: 16, display: "grid", gap: 16 }}>
      <Card title="Templates" extra={<Button type="primary" onClick={() => setOpen(true)}>New</Button>}>
        <Table rowKey="id" loading={loading} dataSource={data} columns={columns as any} pagination={false} />
      </Card>

      <TemplateModal open={open} onCancel={() => setOpen(false)} onSubmit={onCreate} title="Create Template" />
      <TemplateModal open={!!edit} onCancel={() => setEdit(null)} onSubmit={onUpdate} initial={edit ?? undefined} title="Edit Template" />
    </div>
  );
};

export default TemplatesPage;
