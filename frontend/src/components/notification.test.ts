import { vi, test, expect } from 'vitest';
import { notify } from './notification';
import { notification } from 'antd';

vi.mock('antd', () => ({
  notification: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
  },
}));

test('notify forwards to antd notification', () => {
  notify('success', 'ok');
  expect(notification.success).toHaveBeenCalledWith({ message: 'ok', description: undefined });
  notify('error', 'fail', 'bad');
  expect(notification.error).toHaveBeenCalledWith({ message: 'fail', description: 'bad' });
});
