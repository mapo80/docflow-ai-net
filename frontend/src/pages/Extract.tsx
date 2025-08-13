import { useState } from 'react';
import { Button, Input, Upload, message } from 'antd';
import type { UploadFile, UploadProps } from 'antd';
import MDEditor from '@uiw/react-md-editor';
import { useNavigate } from 'react-router-dom';
import { extractData } from '../api';
import type { FieldSpec } from '../api';
import FieldsEditor from '../components/FieldsEditor';
import FullScreenLoader from '../components/FullScreenLoader';

interface ExtractProps {
  apiKey: string;
  onLogout: () => void;
}

export default function Extract({ apiKey, onLogout }: ExtractProps) {
  const [file, setFile] = useState<File | null>(null);
  const [fileList, setFileList] = useState<UploadFile[]>([]);
  const [model, setModel] = useState('');
  const [prompt, setPrompt] = useState('');
  const [fields, setFields] = useState<FieldSpec[]>([]);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const uploadProps: UploadProps = {
    beforeUpload: (f) => {
      setFile(f);
      setFileList([f]);
      return false;
    },
    onRemove: () => {
      setFile(null);
      setFileList([]);
    },
    fileList,
  };

  const handleSubmit = async () => {
    if (!file) {
      message.error('Please upload a file');
      return;
    }
    setLoading(true);
    try {
      const result = await extractData(apiKey, { model, prompt, file, fields });
      navigate('/preview', { state: { file, result } });
    } catch (e) {
      message.error('Extraction failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: 24 }}>
      {loading && <FullScreenLoader />}
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 16 }}>
        <Button onClick={onLogout}>Logout</Button>
      </div>
      <Upload.Dragger
        {...uploadProps}
        multiple={false}
        accept="image/*,.pdf"
        style={{ marginBottom: 16 }}
      >
        <p>Drop file here or click to upload</p>
      </Upload.Dragger>
      <Input
        placeholder="Model name"
        value={model}
        onChange={(e) => setModel(e.target.value)}
        style={{ marginBottom: 16 }}
      />
      <div data-color-mode="light" style={{ marginBottom: 16 }}>
        <MDEditor value={prompt} onChange={(v) => setPrompt(v || '')} height={200} />
      </div>
      <FieldsEditor fields={fields} onChange={setFields} />
      <Button type="primary" style={{ marginTop: 16 }} onClick={handleSubmit}>
        Extract
      </Button>
    </div>
  );
}
