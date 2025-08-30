import { Card, Space, Button } from 'antd';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { RulesEditor, RuleTestsPanel, PropertyReport } from '../rules';
import AiSuggestDrawer from '../rules/components/AiSuggestDrawer';
import FuzzPanel from '../rules/components/FuzzPanel';

export default function RuleEditPage() {
  const [open, setOpen] = useState(false);
  const { id } = useParams();
  if (!id) return null;
  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div style={{ display:'flex', justifyContent:'flex-end' }}>
        <Button type="primary" onClick={()=>setOpen(true)}>AI Suggestions</Button>
      </div>
      <Card>
        <RulesEditor ruleId={id} />
      </Card>
      <Card>
        <RuleTestsPanel ruleId={id} />
      </Card>
      <Card>
        <PropertyReport ruleId={id} />
      </Card>
      <Card>
        <FuzzPanel ruleId={id} />
      </Card>
      <AiSuggestDrawer open={open} onClose={()=>setOpen(false)} ruleId={id} />
    </Space>
  );
}
