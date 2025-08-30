import { Card, Space } from 'antd';
import { useParams } from 'react-router-dom';
import { RulesEditor, RuleTestsPanel, PropertyReport } from '../rules';

export default function RuleEditPage() {
  const { id } = useParams();
  if (!id) return null;
  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <Card>
        <RulesEditor ruleId={id} />
      </Card>
      <Card>
        <RuleTestsPanel ruleId={id} />
      </Card>
      <Card>
        <PropertyReport ruleId={id} />
      </Card>
    </Space>
  );
}
