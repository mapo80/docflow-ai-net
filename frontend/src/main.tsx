import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ConfigProvider, theme } from 'antd';
import 'antd/dist/reset.css';
import './index.css';
import App from './App';
import { HashRouter } from 'react-router-dom';
import { OpenAPI } from './generated';

// Ensure API calls are prefixed with /api/v1
const base = import.meta.env.VITE_API_BASE_URL || '';
const normalized = base.replace(/\/$/, '');
OpenAPI.BASE = normalized.endsWith('/api/v1') ? normalized : `${normalized}/api/v1`;

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ConfigProvider theme={{ algorithm: theme.defaultAlgorithm }}>
      <HashRouter>
        <App />
      </HashRouter>
    </ConfigProvider>
  </StrictMode>,
);
