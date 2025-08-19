import { createContext, useContext, useEffect, type ReactNode } from 'react';
import notify from './notification';

interface ErrorContextValue {
  showError: (message: string) => void;
}

const ErrorContext = createContext<ErrorContextValue>({
  showError: () => {},
});

export function ApiErrorProvider({ children }: { children: ReactNode }) {
  const showError = (msg: string) => notify('error', msg);

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
        notify('error', msg);
      }
      return res;
    };
    return () => {
      window.fetch = originalFetch;
    };
  }, []);

  return <ErrorContext.Provider value={{ showError }}>{children}</ErrorContext.Provider>;
}

export function useApiError() {
  return useContext(ErrorContext);
}

export default ApiErrorProvider;
