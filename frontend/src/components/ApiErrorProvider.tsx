import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { Badge, Space } from 'antd';

interface ErrorContextValue {
  showError: (message: string) => void;
}

const ErrorContext = createContext<ErrorContextValue>({
  showError: () => {},
});

export function ApiErrorProvider({ children }: { children: ReactNode }) {
  const [errors, setErrors] = useState<string[]>([]);

  const showError = (msg: string) => {
    setErrors((prev) => [...prev, msg]);
    setTimeout(() => {
      setErrors((prev) => prev.slice(1));
    }, 5000);
  };

  useEffect(() => {
    const originalFetch = window.fetch;
    window.fetch = async (...args) => {
      const res = await originalFetch(...args);
      if (!res.ok) {
        let msg = res.statusText || 'Request failed';
        try {
          const data = await res.clone().json();
          msg = data?.detail || data?.title || data?.message || msg;
        } catch {
          // ignore
        }
        showError(msg);
      }
      return res;
    };
    return () => {
      window.fetch = originalFetch;
    };
  }, []);

  return (
    <ErrorContext.Provider value={{ showError }}>
      {children}
      <div style={{ position: 'fixed', top: 16, right: 16, zIndex: 1000 }}>
        <Space direction="vertical">
          {errors.map((e, i) => (
            <Badge key={i} status="error" text={e} />
          ))}
        </Space>
      </div>
    </ErrorContext.Provider>
  );
}

export function useApiError() {
  return useContext(ErrorContext);
}

export default ApiErrorProvider;
