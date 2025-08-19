import { notification } from 'antd';
import type { ReactNode } from 'react';
import 'antd/es/notification/style';

export type AppNotificationType = 'success' | 'error' | 'warning';

let apiRef: ReturnType<typeof notification.useNotification>[0] | undefined;

export function NotificationProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [api, contextHolder] = notification.useNotification();
  apiRef = api;
  return (
    <>
      {contextHolder}
      {children}
    </>
  );
}

export function notify(
  type: AppNotificationType,
  message: string,
  description?: string,
): void {
  apiRef?.[type]({ message, description });
}

export default notify;
