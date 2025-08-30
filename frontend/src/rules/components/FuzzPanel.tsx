import { useEffect, useState } from 'react';
import { Button, Card, Drawer, List, Space, Checkbox, Tag, InputNumber, message } from 'antd';
import { FuzzService } from '../../generated/services/FuzzService';

type FuzzItem = { id: string; field: string; label: string; value: any };

export default function FuzzPanel({ ruleId }: { ruleId: string }){
  const [open, setOpen] = useState(false);
  const [maxPerField, setMaxPerField] = useState<number>(5);
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState<FuzzItem[]>([]);
  const [selected, setSelected] = useState<string[]>([]);

  const preview = async () => {
    setLoading(true);
    try {
      const res:any = await FuzzService.postApiV1RulesFuzzPreview({ ruleId, maxPerField });
      const arr: FuzzItem[] = (res?.items || res || []).map((x:any, i:number) => ({
        id: x.id || String(i),
        field: x.field || 'unknown',
        label: x.case || x.label || 'case',
        value: x.value,
      }));
      setItems(arr);
      setSelected(arr.map(x=>x.id));
    } catch(e:any){
      message.error(e?.message || 'Errore anteprima fuzz');
    } finally { setLoading(false); }
  };

  const importSelected = async () => {
    try{
      const sel = items.filter(x=> selected.includes(x.id));
      if(!sel.length){ message.warning('Seleziona almeno un caso'); return; }
      await FuzzService.postApiV1RulesFuzzImport({
        ruleId,
        requestBody: { cases: sel.map(s => ({ field: s.field, label: s.label, value: s.value })) }
      });
      message.success(`Importati ${sel.length} casi come test`);
      setOpen(false);
    } catch(e:any){
      message.error(e?.message || 'Errore import');
    }
  };

  useEffect(()=>{ if(open) { void preview(); } },[open, maxPerField]);

  return (
    <Card type="inner" title="Fuzz & Property" extra={<Button onClick={()=>setOpen(true)}>Open</Button>}>
      <p style={{opacity:0.75, marginBottom:0}}>Esegui generazione di casi di fuzzing per ampliare i test.</p>
      <Drawer title="Fuzz Preview" width={720} open={open} onClose={()=>setOpen(false)} destroyOnClose>
        <Space direction="vertical" size="middle" style={{ width:'100%' }}>
          <Space>
            <span>Max per field:</span>
            <InputNumber min={1} max={50} value={maxPerField} onChange={(v)=> setMaxPerField(Number(v||5))}/>
            <Button onClick={preview} loading={loading}>Refresh</Button>
            <Button type="primary" onClick={importSelected} disabled={!selected.length}>Import selected</Button>
          </Space>
          <List
            bordered
            dataSource={items}
            rowKey="id"
            renderItem={(it)=>(
              <List.Item
                actions={[<Checkbox key="c" checked={selected.includes(it.id)} onChange={(e)=>{
                  const on=e.target.checked;
                  setSelected(prev => on ? [...new Set(prev.concat(it.id))] : prev.filter(x=>x!==it.id));
                }}>Select</Checkbox>]}
              >
                <List.Item.Meta
                  title={<Space><Tag>{it.field}</Tag><span>{it.label}</span></Space>}
                  description={<pre style={{margin:0, whiteSpace:'pre-wrap'}}>{JSON.stringify(it.value, null, 2)}</pre>}
                />
              </List.Item>
            )}
          />
        </Space>
      </Drawer>
    </Card>
  );
}
