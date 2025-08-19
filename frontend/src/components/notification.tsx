import { notification, theme } from 'antd';
import type { ReactNode } from 'react';
import 'antd/es/notification/style';
import {
  CheckCircleFilled,
  CloseCircleFilled,
  ExclamationCircleFilled,
  CloseOutlined,
} from '@ant-design/icons';

export type AppNotificationType = 'success' | 'error' | 'warning';

let apiRef: ReturnType<typeof notification.useNotification>[0] | undefined;
let tokenRef:
  | Pick<
      ReturnType<typeof theme.useToken>['token'],
      'colorWarning' | 'fontWeightStrong'
    >
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
      ? 'var(--success-bg)'
      : type === 'error'
        ? 'var(--error-bg)'
        : tokenRef?.colorWarning;
  const color =
    type === 'success'
      ? 'var(--success-text)'
      : type === 'error'
        ? 'var(--error-text)'
        : '#fff';
  const borderColor =
    type === 'success'
      ? 'var(--success-border)'
      : type === 'error'
        ? 'var(--error-border)'
        : undefined;
  const iconColor =
    type === 'success'
      ? 'var(--success-icon)'
      : type === 'error'
        ? 'var(--error-icon)'
        : '#fff';
  const icon =
    type === 'success'
      ? <CheckCircleFilled style={{ color: iconColor }} />
      : type === 'error'
        ? <CloseCircleFilled style={{ color: iconColor }} />
        : <ExclamationCircleFilled style={{ color: iconColor }} />;
  apiRef?.open({
    type,
    message,
    description,
    duration: 2,
    showProgress: true,
    style: {
      backgroundColor,
      color,
      fontWeight: tokenRef?.fontWeightStrong ?? 'bold',
      border: borderColor ? `1px solid ${borderColor}` : undefined,
    },
    icon,
    closeIcon: <CloseOutlined />,
  });
}

export default notify;
