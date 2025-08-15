import { useEffect, useState } from 'react';
import { Alert } from 'antd';

type Props = {
  seconds: number;
  onFinish?: () => void;
};

export default function RetryAfterBanner({ seconds, onFinish }: Props) {
  const [remaining, setRemaining] = useState(seconds);
  useEffect(() => {
    setRemaining(seconds);
  }, [seconds]);
  useEffect(() => {
    if (remaining <= 0) {
      onFinish?.();
      return;
    }
    const id = setTimeout(() => setRemaining((s) => s - 1), 1000);
    return () => clearTimeout(id);
  }, [remaining, onFinish]);
  if (remaining <= 0) return null;
  return (
    <Alert
      banner
      type="warning"
      message={`Retry in ${remaining}s`}
    />
  );
}
