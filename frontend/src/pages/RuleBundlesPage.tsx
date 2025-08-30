import { useEffect, useState } from 'react';
import { Button, Card, Checkbox, List, Space, Upload, message } from 'antd';
import { RulesService } from '../generated/services/RulesService';
import { RuleTestsService } from '../generated/services/RuleTestsService';
import { DownloadOutlined, UploadOutlined } from '@ant-design/icons';

type RuleRow = { id: string; name: string; selected: boolean; testsCount?: number };

export default function RuleBundlesPage(){
  const [rows, setRows] = useState<RuleRow[]>([]);
  const [loading, setLoading] = useState(false);

  const load = async () => {
    const data:any = await RulesService.getApiV1Rules();
    const arr = (data?.items||data||[]).map((r:any)=> ({ id: r.id, name: r.name, selected: false }));
    setRows(arr);
  };
  useEffect(()=>{ void load(); },[]);

  const toggle = (id:string, on:boolean)=> setRows(prev=> prev.map(r=> r.id===id? {...r, selected:on}: r));

  const exportPkg = async () => {
    try {
      setLoading(true);
      const selected = rows.filter(r=>r.selected);
      if(!selected.length){ message.warning('Seleziona almeno una regola'); return;}
      const pkg:any = { schema:'docflow.rulepkg@v1', createdAt: new Date().toISOString(), rules: [] as any[] };
      for(const r of selected){
        const rule:any = await RulesService.getApiV1Rules1({ id: r.id });
        const tests:any = await RuleTestsService.getApiV1RulesTests({ ruleId: r.id, page:1, pageSize:1000 });
        pkg.rules.push({ rule, tests: tests?.items || tests });
      }
      const blob = new Blob([JSON.stringify(pkg,null,2)], { type: 'application/json' });
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = 'export.rulepkg.json';
      a.click();
      URL.revokeObjectURL(a.href);
      message.success('Export completato');
    } catch(e:any){ message.error(e?.message||'Errore export'); } finally { setLoading(false); }
  };

  const importPkg = async (content: string) => {
    try {
      const pkg = JSON.parse(content);
      if(pkg.schema !== 'docflow.rulepkg@v1'){ message.error('Schema non supportato'); return; }
      for(const item of pkg.rules){
        const r = item.rule;
        try {
          const created:any = await RulesService.postApiV1Rules({ requestBody: { name: r.name + ' (import)', description: r.description, code: r.code, enabled: !!r.enabled, readsCsv: r.readsCsv, writesCsv: r.writesCsv } });
          const newId = created?.id;
          for(const t of (item.tests||[])){
            await RuleTestsService.postApiV1RulesTests({ ruleId: newId, requestBody: { name: t.name, input: t.input, expect: t.expect, suite: t.suite, tags: t.tags } as any });
          }
        } catch(err:any){
          console.warn('Import error for rule', r?.name, err);
        }
      }
      message.success('Import completato');
    } catch{ message.error('File non valido'); }
  };

  return (
    <Space direction="vertical" size="large" style={{ width:'100%' }}>
      <Card title="Export rules/tests">
        <List
          dataSource={rows}
          rowKey="id"
          renderItem={(r)=>(
            <List.Item actions={[<Checkbox checked={r.selected} onChange={e=>toggle(r.id, e.target.checked)}>Seleziona</Checkbox>]}>
              <List.Item.Meta title={r.name} description={r.id} />
            </List.Item>
          )}
        />
        <Button icon={<DownloadOutlined/>} type="primary" onClick={exportPkg} loading={loading}>Export .rulepkg.json</Button>
      </Card>

      <Card title="Import bundle">
        <Upload
          accept=".json,.rulepkg,.rulepkg.json"
          showUploadList={false}
          beforeUpload={(file)=>{
            const reader = new FileReader();
            reader.onload = () => importPkg(String(reader.result||''));
            reader.readAsText(file);
            return false;
          }}
        >
          <Button icon={<UploadOutlined/>}>Importa file</Button>
        </Upload>
      </Card>
    </Space>
  );
}
