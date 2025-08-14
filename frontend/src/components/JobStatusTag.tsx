import { Tag } from 'antd';

export const statusColor: Record<string, string> = {
  Queued: 'default',
  Running: 'blue',
  Succeeded: 'green',
  Failed: 'red',
  Cancelled: 'orange',
};

export default function JobStatusTag({ status, derived }: { status: string; derived?: string | null }) {
  return (
    <div>
      <Tag color={statusColor[status] || 'default'}>{status}</Tag>
      {derived && <small style={{ marginLeft: 4 }}>{derived}</small>}
    </div>
  );
}
