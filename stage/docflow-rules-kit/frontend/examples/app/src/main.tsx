import React from 'react'
import ReactDOM from 'react-dom/client'
import { ConfigProvider, App as AntApp } from 'antd'
import 'antd/dist/reset.css'
import App from './ui/App'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ConfigProvider>
      <AntApp><App/></AntApp>
    </ConfigProvider>
  </React.StrictMode>
)

// In dev, set API key via .env: VITE_API_KEY=dev-api-key
