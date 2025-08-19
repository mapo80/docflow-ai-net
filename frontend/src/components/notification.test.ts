import { vi, test, expect } from 'vitest';
import { render } from '@testing-library/react';
import { notify, NotificationProvider } from './notification';

const api = { open: vi.fn() };

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
  notify('error', 'fail', 'bad');
  expect(api.open).toHaveBeenCalledTimes(2);
  const first = api.open.mock.calls[0][0];
  expect(first).toMatchObject({
    type: 'success',
    message: 'ok',
    description: undefined,
    duration: 2,
    showProgress: true,
    style: { backgroundColor: 'green', color: 'white' },
  });
  expect(first.icon.props.style).toEqual({ color: 'white' });
  expect(first.closeIcon.props.style).toEqual({ color: 'white' });
  const second = api.open.mock.calls[1][0];
  expect(second).toMatchObject({
    type: 'error',
    message: 'fail',
    description: 'bad',
    duration: 2,
    showProgress: true,
    style: { backgroundColor: 'red', color: 'white' },
  });
  expect(second.icon.props.style).toEqual({ color: 'white' });
  expect(second.closeIcon.props.style).toEqual({ color: 'white' });
});
