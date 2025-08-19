import { notification } from 'antd';

export type AppNotificationType = 'success' | 'error' | 'warning';

export function notify(
  type: AppNotificationType,
  message: string,
  description?: string,
): void {
  notification[type]({ message, description });
}

export default notify;
