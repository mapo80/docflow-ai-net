
import LoadingOutlined from '@ant-design/icons/LoadingOutlined';

/** Full-viewport loader for lazy chunk loading (Suspense fallback) */
export default function ChunkLoader() {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
      }}
      aria-label="chunk-loading"
    >
      <LoadingOutlined style={{ fontSize: 48 }} spin />
    </div>
  );
}
