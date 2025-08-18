import { useEffect, useState } from 'react';
import { Modal, Spin } from 'antd';
import { ModelsService } from '../generated';

interface Props {
  open: boolean;
  modelId: string;
  onClose: () => void;
}

export default function ModelLogModal({ open, modelId, onClose }: Props) {
  const [content, setContent] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    ModelsService.modelsDownloadLog({ id: modelId })
      .then((text) => setContent(text))
      .catch(() => setContent(''))
      .finally(() => setLoading(false));
  }, [open, modelId]);

  return (
    <Modal
      open={open}
      onCancel={onClose}
      footer={null}
      title="Download Log"
      width="100%"
      style={{ top: 0 }}
      bodyStyle={{ height: 'calc(100vh - 108px)', overflowY: 'auto' }}
      destroyOnClose
    >
      {loading ? <Spin /> : <pre style={{ whiteSpace: 'pre-wrap' }}>{content}</pre>}
    </Modal>
  );
}
