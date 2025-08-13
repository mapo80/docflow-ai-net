import { Spin } from 'antd';

export default function FullScreenLoader() {
  return (
    <div
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'rgba(255, 255, 255, 0.7)',
        zIndex: 1000,
      }}
    >
      <Spin size="large" />
    </div>
  );
}
