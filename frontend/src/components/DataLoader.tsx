
import { Spin } from 'antd';

/** Content-area loader for REST/data fetch states (not full-viewport) */
export default function DataLoader() {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '40vh',
        width: '100%',
      }}
      aria-label="data-loading"
    >
      <Spin size="large" />
    </div>
  );
}
