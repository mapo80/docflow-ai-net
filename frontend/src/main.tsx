import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ConfigProvider, theme } from 'antd';
import 'antd/dist/reset.css';
import './index.css';
import App from './App';
import { HashRouter } from 'react-router-dom';
import { OpenAPI } from './generated';

const base = import.meta.env.VITE_API_BASE_URL || '';
OpenAPI.BASE = base.replace(/\/$/, '');

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ConfigProvider theme={{ algorithm: theme.defaultAlgorithm }}>
      <HashRouter>
        <App />
      </HashRouter>
    </ConfigProvider>
  </StrictMode>,
);
