import { useEffect, useState, type ReactNode } from 'react';
import Loader from './Loader';

export const globalLoader = {
  show: () => {},
  hide: () => {},
};

export function GlobalLoaderProvider({ children }: { children: ReactNode }) {
  const [visible, setVisible] = useState(false);
  useEffect(() => {
    globalLoader.show = () => setVisible(true);
    globalLoader.hide = () => setVisible(false);
  }, []);
  return (
    <>
      {visible && <Loader />}
      {children}
    </>
  );
}

export function withLoader<T>(factory: () => Promise<T>): () => Promise<T> {
  return async () => {
    globalLoader.show();
    try {
      return await factory();
    } finally {
      globalLoader.hide();
    }
  };
}
