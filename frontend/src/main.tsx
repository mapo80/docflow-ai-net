import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ConfigProvider, theme } from 'antd';
import 'antd/dist/reset.css';
import './index.css';
import App from './App';
import { HashRouter } from 'react-router-dom';
import { OpenAPI } from './generated';

// Use relative API paths when VITE_API_BASE_URL is not provided
OpenAPI.BASE = import.meta.env.VITE_API_BASE_URL || '';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ConfigProvider theme={{ algorithm: theme.defaultAlgorithm }}>
      <HashRouter>
        <App />
      </HashRouter>
    </ConfigProvider>
  </StrictMode>,
);
