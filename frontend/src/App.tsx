import { useState, useEffect, lazy, Suspense } from 'react';
import Loader from './components/Loader';
import { Routes, Route, Navigate } from 'react-router-dom';
import ApiErrorProvider from './components/ApiErrorProvider';
const Login = lazy(() => import('./pages/Login'));
const Shell = lazy(() => import('./Shell'));
const JobsList = lazy(() => import('./pages/JobsList'));
const JobNew = lazy(() => import('./pages/JobNew'));
const JobDetail = lazy(() => import('./pages/JobDetail'));
const HealthPage = lazy(() => import('./pages/HealthPage'));
const ModelManagerPage = lazy(() => import('./pages/ModelManagerPage'));
const ModelsPage = lazy(() => import('./pages/ModelsPage'));
const TemplatesList = lazy(() => import('./pages/TemplatesList'));
const MarkdownPage = lazy(() => import('./pages/MarkdownPage'));
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
    return (
      <Suspense fallback={<Loader />}>
        <Login onLogin={handleLogin} />
      </Suspense>
    );
  }
  return (
    <ApiErrorProvider>
      <Suspense fallback={<Loader />}>
        <Routes>
          <Route path="/" element={<Shell />}>
            <Route index element={<Navigate to="/jobs" />} />
            <Route path="jobs" element={<JobsList />} />
            <Route path="jobs/new" element={<JobNew />} />
            <Route path="jobs/:id" element={<JobDetail />} />
            <Route path="health" element={<HealthPage />} />
            <Route path="model" element={<ModelManagerPage />} />
            <Route path="models" element={<ModelsPage />} />
            <Route path="templates" element={<TemplatesList />} />
            <Route path="markdown" element={<MarkdownPage />} />
            <Route path="*" element={<Navigate to="/jobs" />} />
          </Route>
        </Routes>
      </Suspense>
    </ApiErrorProvider>
  );
}

export default App;
