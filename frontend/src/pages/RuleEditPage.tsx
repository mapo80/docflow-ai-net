import { Card } from 'antd';
import { useParams } from 'react-router-dom';
import { RulesEditor, RuleTestsPanel, PropertyReport } from '../rules';

export default function RuleEditPage() {
  const { id } = useParams();
  if (!id) return null;
  return (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: 16 }}>
      <Card>
        <RulesEditor ruleId={id} />
      </Card>
      <Card>
        <RuleTestsPanel ruleId={id} />
      </Card>
      <Card>
        <PropertyReport ruleId={id} />
      </Card>
    </div>
  );
}
