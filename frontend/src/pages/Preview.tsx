import { useLocation, useNavigate, Navigate } from 'react-router-dom';
import { Button } from 'antd';
import type { JSX } from 'react';

export default function Preview() {
  const navigate = useNavigate();
  const location = useLocation();
  const state = location.state as { file: File; result: unknown } | undefined;

  if (!state) {
    return <Navigate to="/" />;
  }

  const fileUrl = URL.createObjectURL(state.file);
  let preview: JSX.Element;
  if (state.file.type === 'application/pdf') {
    preview = <iframe src={fileUrl} style={{ width: '100%', height: 400 }} title="preview" />;
  } else if (state.file.type.startsWith('image/')) {
    preview = <img src={fileUrl} alt="preview" style={{ maxWidth: '100%', maxHeight: 400 }} />;
  } else {
    preview = <p>Preview not available</p>;
  }

  return (
    <div style={{ padding: 24 }}>
      <Button onClick={() => navigate('/')}>New Request</Button>
      <div style={{ marginTop: 16, marginBottom: 16 }}>{preview}</div>
      <pre>{JSON.stringify(state.result, null, 2)}</pre>
    </div>
  );
}
