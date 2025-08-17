import LoadingOutlined from '@ant-design/icons/LoadingOutlined';

export default function Loader() {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
      }}
    >
      <LoadingOutlined
        aria-label="loading"
        data-testid="loader"
        style={{ fontSize: 48 }}
        spin
      />
    </div>
  );
}
