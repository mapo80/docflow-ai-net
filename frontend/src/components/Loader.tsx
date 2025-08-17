import LoadingOutlined from '@ant-design/icons/LoadingOutlined';

export default function Loader() {
  return (
    <div
      style={{
        position: 'absolute',
        inset: 0,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
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
