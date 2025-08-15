import { useState, useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Shell from './Shell';
import JobsList from './pages/JobsList';
import JobNew from './pages/JobNew';
import JobDetail from './pages/JobDetail';
import HealthPage from './pages/HealthPage';
import ModelManagerPage from './pages/ModelManagerPage';
import { OpenAPI } from './generated';

function App() {
  const [apiKey, setApiKey] = useState<string>(() => localStorage.getItem('apiKey') || '');
  useEffect(() => {
    OpenAPI.HEADERS = apiKey ? { 'X-API-Key': apiKey } : undefined;
  }, [apiKey]);
  const handleLogin = (key: string) => {
    setApiKey(key);
    localStorage.setItem('apiKey', key);
  };
  if (!apiKey) {
    return <Login onLogin={handleLogin} />;
  }
  return (
    <Routes>
      <Route path="/" element={<Shell />}>
        <Route index element={<Navigate to="/jobs" />} />
        <Route path="jobs" element={<JobsList />} />
        <Route path="jobs/new" element={<JobNew />} />
        <Route path="jobs/:id" element={<JobDetail />} />
        <Route path="health" element={<HealthPage />} />
        <Route path="model" element={<ModelManagerPage />} />
        <Route path="*" element={<Navigate to="/jobs" />} />
      </Route>
    </Routes>
  );
}

export default App;
