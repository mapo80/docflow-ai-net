import { useState } from 'react';
import { Drawer, Form, Input, Slider, Button, Space, List, Checkbox, Tag } from 'antd';
import { AiTestsService } from '../../generated/services/AiTestsService';
import { notify } from '../../components/notification';

type Suggestion = { id: string; name: string; input: any; expect: any; score?: number };

export default function AiSuggestDrawer({ open, onClose, ruleId }: { open: boolean; onClose: ()=>void; ruleId: string }){
  const [prompt, setPrompt] = useState('Genera 6 test robusti per normalizzare l\'IBAN: rimuovi separatori, uppercase, idempotenza.');
  const [budget, setBudget] = useState<number>(8000);
  const [temperature, setTemperature] = useState<number>(0.2);
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<Suggestion[]>([]);
  const [selected, setSelected] = useState<string[]>([]);

  const run = async () => {
    try{
      setLoading(true);
      const res:any = await AiTestsService.postApiV1AiTestsSuggest({ ruleId, requestBody: { userPrompt: prompt, budget, temperature } });
      const arr: Suggestion[] = (res?.suggestions||[]).map((x:any)=> ({
        id: x.id || crypto.randomUUID(),
        name: x.name || 'test',
        input: x.input,
        expect: x.expect,
        score: x.score,
      }));
      setItems(arr);
      setSelected(arr.map(x=>x.id));
    }catch(e:any){
      notify('error', e?.message || 'Errore generazione suggerimenti');
    }finally{ setLoading(false); }
  };

  const importSelected = async () => {
    try{
      if(!selected.length){ notify('warning','Seleziona almeno un test'); return; }
      await AiTestsService.postApiV1AiTestsImport({ ruleId, requestBody: { ids: selected, suite: 'ai-generated', tags: ['ai','suggested'] } });
      notify('success', `Importati ${selected.length} test.`);
      onClose();
    }catch(e:any){
      notify('error', e?.message || 'Errore import');
    }
  };

  return (
    <Drawer title="AI Suggestions" width={640} open={open} onClose={onClose} destroyOnClose>
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <Form layout="vertical">
          <Form.Item label="Prompt">
            <Input.TextArea rows={4} value={prompt} onChange={e=>setPrompt(e.target.value)} />
          </Form.Item>
          <Form.Item label={`Budget tokens: ${budget}`}>
            <Slider min={1000} max={32000} step={500} value={budget} onChange={v=>setBudget(v as number)} />
          </Form.Item>
          <Form.Item label={`Temperature: ${temperature}`}>
            <Slider min={0} max={1} step={0.05} value={temperature} onChange={v=>setTemperature(v as number)} />
          </Form.Item>
          <Space>
            <Button onClick={run} loading={loading} type="primary">Generate</Button>
            <Button onClick={importSelected} disabled={!selected.length}>Import selected</Button>
          </Space>
        </Form>

        <List
          bordered
          dataSource={items}
          rowKey="id"
          renderItem={(it)=> (
            <List.Item
              actions={[<Checkbox key="sel" checked={selected.includes(it.id)} onChange={(e)=>{
                const on=e.target.checked;
                setSelected(prev=> on? [...new Set(prev.concat(it.id))] : prev.filter(x=>x!==it.id));
              }}>Select</Checkbox>]}
            >
              <List.Item.Meta
                title={<Space><span>{it.name}</span>{it.score!=null && <Tag color="blue">score {it.score}</Tag>}</Space>}
                description={<pre style={{ whiteSpace:'pre-wrap', margin:0 }}>{JSON.stringify({ input: it.input, expect: it.expect }, null, 2)}</pre>}
              />
            </List.Item>
          )}
        />
      </Space>
    </Drawer>
  );
}
