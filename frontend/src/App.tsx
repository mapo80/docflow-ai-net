import { useState } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import ModelManager from './pages/ModelManager';
import Extract from './pages/Extract';
import Preview from './pages/Preview';

function App() {
  const [apiKey, setApiKey] = useState<string>(() => localStorage.getItem('apiKey') || '');

  const handleLogin = (key: string) => {
    setApiKey(key);
    localStorage.setItem('apiKey', key);
  };

  const handleLogout = () => {
    setApiKey('');
    localStorage.removeItem('apiKey');
  };

  if (!apiKey) {
    return <Login onLogin={handleLogin} />;
  }

  return (
    <Routes>
      <Route path="/" element={<Extract apiKey={apiKey} onLogout={handleLogout} />} />
      <Route path="/models" element={<ModelManager apiKey={apiKey} onLogout={handleLogout} />} />
      <Route path="/preview" element={<Preview />} />
      <Route path="*" element={<Navigate to="/" />} />
    </Routes>
  );
}

export default App;
