import DataLoader from "@/components/DataLoader";
import React, { useEffect, useState } from "react";
import { Card, Button, message } from "antd";
import AddModelModal from "@/components/models/AddModelModal";
import ModelTable from "@/components/models/ModelTable";
import api, { type AddModelRequest, type ModelDto } from "@/services/modelsApi";

const ModelsPage: React.FC = () => {
  const [models, setModels] = useState<ModelDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);

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

  const onAdd = async (req: AddModelRequest) => {
    try {
      await api.add(req);
      message.success("Model added");
      await load();
    } catch (e: any) {
      message.error(e.message ?? "Failed to add");
    }
  };

  const onDownload = async (id: string) => {
    try {
      await api.download(id);
      message.success("Download started");
      await load();
    } catch (e: any) {
      message.error(e.message ?? "Failed to start download");
    }
  };

  const onActivate = async (id: string) => {
    try {
      await api.activate(id);
      message.success("Model activated");
      await load();
    } catch (e: any) {
      message.error(e.message ?? "Failed to activate");
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
      <Card title="GGUF Models" extra={<Button type="primary" onClick={() => setOpen(true)}>Add</Button>}>
        <ModelTable data={models} onDownload={onDownload} onActivate={onActivate} onDelete={onDelete} loading={loading} />
      </Card>
      <AddModelModal open={open} onCancel={() => setOpen(false)} onSubmit={onAdd} />
    </div>
  );
};

export default ModelsPage;
