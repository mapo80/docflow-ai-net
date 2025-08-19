import { vi, test, expect } from 'vitest';
import { render } from '@testing-library/react';
import { notify, NotificationProvider } from './notification';

const api = {
  success: vi.fn(),
  error: vi.fn(),
  warning: vi.fn(),
};

vi.mock('antd', () => ({
  notification: {
    useNotification: () => [api, <div key="n" />],
  },
  theme: {
    useToken: () => ({
      token: {
        colorSuccess: 'green',
        colorError: 'red',
        colorWarning: 'orange',
        colorTextLightSolid: 'white',
      },
    }),
  },
}));

test('notify forwards to antd notification', () => {
  render(
    <NotificationProvider>
      <div />
    </NotificationProvider>,
  );
  notify('success', 'ok');
  expect(api.success).toHaveBeenCalledWith({
    message: 'ok',
    description: undefined,
    duration: 2,
    showProgress: true,
    style: { backgroundColor: 'green', color: 'white' },
  });
  notify('error', 'fail', 'bad');
  expect(api.error).toHaveBeenCalledWith({
    message: 'fail',
    description: 'bad',
    duration: 2,
    showProgress: true,
    style: { backgroundColor: 'red', color: 'white' },
  });
});
