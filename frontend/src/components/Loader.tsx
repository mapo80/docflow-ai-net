import LoadingOutlined from '@ant-design/icons/LoadingOutlined';

export default function Loader() {
  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#fff',
        zIndex: 1000,
      }}
    >
      <LoadingOutlined aria-label="loading" style={{ fontSize: 48 }} spin />
    </div>
  );
}
