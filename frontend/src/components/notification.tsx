import { notification, theme } from 'antd';
import type { ReactNode } from 'react';
import 'antd/es/notification/style';

export type AppNotificationType = 'success' | 'error' | 'warning';

let apiRef: ReturnType<typeof notification.useNotification>[0] | undefined;
let tokenRef:
  | Pick<ReturnType<typeof theme.useToken>['token'],
      'colorSuccess' | 'colorError' | 'colorWarning' | 'colorTextLightSolid'>
  | undefined;

export function NotificationProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [api, contextHolder] = notification.useNotification();
  const { token } = theme.useToken();
  apiRef = api;
  tokenRef = token;
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
  const backgroundColor =
    type === 'success'
      ? tokenRef?.colorSuccess
      : type === 'error'
        ? tokenRef?.colorError
        : tokenRef?.colorWarning;
  apiRef?.[type]({
    message,
    description,
    duration: 2,
    showProgress: true,
    style: {
      backgroundColor,
      color: tokenRef?.colorTextLightSolid,
    },
  });
}

export default notify;
