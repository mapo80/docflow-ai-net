import DataLoader from "@/components/DataLoader";
import React, { useEffect, useState } from "react";
import { Card, Button, message } from "antd";
import ModelModal from "@/components/models/ModelModal";
import ModelTable from "@/components/models/ModelTable";
import api, { type AddModelRequest, type ModelDto } from "@/services/modelsApi";

const ModelsPage: React.FC = () => {
  const [models, setModels] = useState<ModelDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<ModelDto | null>(null);

  const load = async () => {
    setLoading(true);
    try {
      const data = await api.list();
      setModels(data);
    } catch (e: any) {
      message.error(e.message ?? "Failed to load models");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    const id = setInterval(load, 2000);
    return () => clearInterval(id);
  }, []);

  const onSave = async (req: AddModelRequest) => {
    try {
      if (editing) {
        await api.update(editing.id, req);
        message.success("Model updated");
      } else {
        await api.add(req);
        message.success("Model added");
      }
      await load();
    } catch (e: any) {
      message.error(e.message ?? "Failed to save");
    }
  };

  const onDownload = async (id: string) => {
    try {
      await api.startDownload(id);
      message.success("Download started");
      await load();
    } catch (e: any) {
      message.error(e.message ?? "Failed to start download");
    }
  };

  const onDelete = async (id: string) => {
    try {
      await api.remove(id);
      message.success("Model deleted");
      await load();
    } catch (e: any) {
      message.error(e.message ?? "Failed to delete");
    }
  };

  if (loading) { return <DataLoader />; }

  return (
    <div style={{ padding: 16, display: "grid", gap: 16 }}>
      <Card title="Models" extra={<Button type="primary" onClick={() => { setEditing(null); setOpen(true); }}>Add</Button>}>
        <ModelTable data={models} onDownload={onDownload} onDelete={onDelete} onEdit={(m) => { setEditing(m); setOpen(true); }} loading={loading} />
      </Card>
      <ModelModal open={open} model={editing} onCancel={() => { setOpen(false); setEditing(null); }} onSubmit={onSave} />
    </div>
  );
};

export default ModelsPage;
