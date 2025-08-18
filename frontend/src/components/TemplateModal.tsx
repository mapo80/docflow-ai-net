import { useState, useEffect } from 'react';
import { Modal, Input, Button, Space, Grid } from 'antd';
import MDEditor from '@uiw/react-md-editor';
import dayjs from 'dayjs';
import TemplateFieldsEditor from './TemplateFieldsEditor';
import type { SimpleField } from '../templates/fieldsConversion';
import { slugify, isSlug, simpleToObject, objectToSimple } from '../templates/fieldsConversion';
import { TemplatesService } from '../generated';

interface Props {
  open: boolean;
  templateId?: string;
  onClose: (changed: boolean) => void;
}

export default function TemplateModal({ open, templateId, onClose }: Props) {
  const [name, setName] = useState('');
  const [token, setToken] = useState('');
  const [prompt, setPrompt] = useState<string | undefined>('');
  const [fields, setFields] = useState<SimpleField[]>([]);
  const [createdAt, setCreatedAt] = useState<string | null>(null);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [jsonError, setJsonError] = useState<string | null>(null);
  const screens = Grid.useBreakpoint();
  const isMobile = !screens.md;

  useEffect(() => {
    if (!open) return;
    if (templateId) {
      TemplatesService.templatesGet({ id: templateId }).then((t) => {
        setName(t.name || '');
        setToken(t.token || '');
        setPrompt(t.promptMarkdown || '');
        setFields(objectToSimple(t.fieldsJson as any));
        setCreatedAt(t.createdAt || null);
        setUpdatedAt(t.updatedAt || null);
      });
    } else {
      setName('');
      setToken('');
      setPrompt('');
      setFields([]);
      setCreatedAt(null);
      setUpdatedAt(null);
    }
  }, [open, templateId]);

  const hasEmptyKey = fields.some((f) => !f.key);
  const duplicates = fields.some(
    (f, i) => fields.findIndex((x) => x.key === f.key && x.value === f.value) !== i,
  );
  const invalid =
    !name || !isSlug(token) || hasEmptyKey || duplicates || jsonError !== null;

  const handleSave = async () => {
    const payload = {
      name,
      token,
      promptMarkdown: prompt || null,
      fieldsJson: simpleToObject(fields),
    } as any;
    setLoading(true);
    try {
      if (templateId) {
        await TemplatesService.templatesUpdate({ id: templateId, requestBody: payload });
      } else {
        await TemplatesService.templatesCreate({ requestBody: payload });
      }
      onClose(true);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      open={open}
      title={templateId ? 'Edit Template' : 'Create Template'}
      onCancel={() => onClose(false)}
      width="100%"
      style={{ top: 0, padding: 0 }}
      bodyStyle={{ height: 'calc(100vh - 110px)', overflow: 'auto' }}
      footer={
        <Space>
          <Button onClick={() => onClose(false)}>Cancel</Button>
          <Button type="primary" onClick={handleSave} disabled={invalid} loading={loading}>
            Save
          </Button>
        </Space>
      }
      destroyOnClose
    >
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <Input
          placeholder="Template Name"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <Space direction={isMobile ? 'vertical' : 'horizontal'} style={{ width: '100%' }}>
          <Input
            placeholder="Template Token"
            value={token}
            onChange={(e) => setToken(e.target.value)}
          />
          <Button onClick={() => setToken(slugify(name))}>Auto-generate from name</Button>
        </Space>
        <MDEditor value={prompt} onChange={setPrompt} />
        <TemplateFieldsEditor value={fields} onChange={setFields} onJsonError={setJsonError} />
        {templateId && (
          <Space direction={isMobile ? 'vertical' : 'horizontal'} style={{ width: '100%' }}>
            <Input
              value={createdAt ? dayjs(createdAt).format('YYYY-MM-DD HH:mm') : ''}
              disabled
              placeholder="Created At"
            />
            <Input
              value={updatedAt ? dayjs(updatedAt).format('YYYY-MM-DD HH:mm') : ''}
              disabled
              placeholder="Last Updated"
            />
          </Space>
        )}
      </Space>
    </Modal>
  );
}
