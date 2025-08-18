import { useState, useEffect } from 'react';
import { Modal, Input, Button, Space, Grid } from 'antd';
import MDEditor from '@uiw/react-md-editor';
import TemplateFieldsEditor from './TemplateFieldsEditor';
import type { FieldItem } from './FieldsEditor';
import { fieldsToJson, jsonToFields } from './FieldsEditor';
import { slugify, isSlug } from '../templates/slug';
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
  const [fields, setFields] = useState<FieldItem[]>([]);
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
        try {
          setFields(jsonToFields(JSON.stringify(t.fieldsJson ?? { fields: [] })));
        } catch {
          setFields([]);
        }
      });
    } else {
      setName('');
      setToken('');
      setPrompt('');
      setFields([]);
    }
  }, [open, templateId]);

  const hasEmptyName = fields.some((f) => !f.name);
  const duplicates = fields.some(
    (f, i) => fields.findIndex((x) => x.name === f.name && x.type === f.type) !== i,
  );
  const invalid =
    !name || !isSlug(token) || hasEmptyName || duplicates || jsonError !== null;

  const handleSave = async () => {
    const payload = {
      name,
      token,
      promptMarkdown: prompt || null,
      fieldsJson: JSON.parse(fieldsToJson(fields)),
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
        <div>
          <label htmlFor="tpl-name">Template Name</label>
          <Input
            id="tpl-name"
            placeholder="Template Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>
        <div>
          <label htmlFor="tpl-token">Template Token</label>
          <Space direction={isMobile ? 'vertical' : 'horizontal'} style={{ width: '100%' }}>
            <Input
              id="tpl-token"
              placeholder="Template Token"
              value={token}
              onChange={(e) => setToken(e.target.value)}
            />
            <Button onClick={() => setToken(slugify(name))}>Auto-generate from name</Button>
          </Space>
        </div>
        <div>
          <label htmlFor="tpl-prompt">Prompt</label>
          <MDEditor id="tpl-prompt" value={prompt} onChange={setPrompt} preview="edit" />
        </div>
        <div>
          <label>Fields</label>
          <TemplateFieldsEditor value={fields} onChange={setFields} onJsonError={setJsonError} />
        </div>
      </Space>
    </Modal>
  );
}
