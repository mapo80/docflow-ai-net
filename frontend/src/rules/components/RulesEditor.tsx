import { useEffect, useRef, useState } from 'react';
import { Badge, Button, Card, Descriptions, Space, message } from 'antd';
import Editor, { type OnMount } from '@monaco-editor/react';
import { RulesService } from '../../generated/services/RulesService';
import { syncWorkspace } from '../lsp';

export interface RuleDetail {
  id: string;
  name: string;
  version: string;
  isBuiltin: boolean;
  enabled: boolean;
  code: string;
  updatedAt: string;
  readsCsv?: string;
  writesCsv?: string;
}

export interface RulesEditorProps {
  ruleId: string;
}

export default function RulesEditor({ ruleId }: RulesEditorProps) {
  const editorRef = useRef<any>(null);
  const monacoRef = useRef<any>(null);
  const insertSnippet = (snippet: string) => {
    const ed = editorRef.current;
    const mon = monacoRef.current;
    if (!ed || !mon) return;
    const sel = ed.getSelection();
    const pos = sel ? sel.getStartPosition() : ed.getPosition();
    ed.executeEdits('snippet', [{ range: new mon.Range(pos.lineNumber, pos.column, pos.lineNumber, pos.column), text: snippet }]);
    ed.focus();
  };
  const [rule, setRule] = useState<RuleDetail | null>(null);
  const [code, setCode] = useState('');
  const [saving, setSaving] = useState(false);
  const [synced, setSynced] = useState(true);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    RulesService.getApiV1Rules1({ id: ruleId }).then((r: any) => {
      setRule(r as RuleDetail);
      setCode((r as any).code || '');
    });
  }, [ruleId]);

  const handleSave = async () => {
    if (!rule) return;
    await RulesService.putApiV1Rules({ id: ruleId, requestBody: { ...rule, code } });
    message.success('Rule saved');
    const r = await RulesService.getApiV1Rules1({ id: ruleId });
    setRule(r as RuleDetail);
  };

  const handleCompile = async () => {
    const res: any = await RulesService.postApiV1RulesCompile({ id: ruleId });
    if (res?.ok) {
      message.success('Compile succeeded');
    } else {
      message.error((res?.errors || []).join('\n'));
    }
  };

  const handleChange = (value?: string) => {
    const v = value || '';
    setCode(v);
    setSaving(true);
    setSynced(false);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(async () => {
      await syncWorkspace(ruleId, v);
      setSaving(false);
      setSynced(true);
    }, 500);
  };

  const handleMount: OnMount = (editor, monaco) => {
    editorRef.current = editor;
    monacoRef.current = monaco;
    const uri = monaco.Uri.parse('inmemory://rule.csx');
    let model = monaco.editor.getModel(uri);
    if (!model) {
      model = monaco.editor.createModel(code, 'csharp', uri);
    }
    editor.setModel(model);
  };

  if (!rule) return null;

  return (
    <Card
      title={rule.name}
      extra={
        <Space>
          <Space style={{marginBottom:12}} wrap>
          <span style={{opacity:0.7}}>Insert snippet:</span>
          <Button size="small" onClick={()=>insertSnippet('var s = g.Get<string>("iban") ?? string.Empty;\n')}>Get iban</Button>
          <Button size="small" onClick={()=>insertSnippet('s = System.Text.RegularExpressions.Regex.Replace(s, "[^A-Z0-9]+", "");\n')}>Regex strip non-alnum</Button>
          <Button size="small" onClick={()=>insertSnippet('s = s.ToUpperInvariant();\n')}>Uppercase</Button>
          <Button size="small" onClick={()=>insertSnippet('g.Set("iban", s);\n')}>Set iban</Button>
        </Space>
        <EditorSyncBadge saving={saving} synced={synced} />
          <Button onClick={handleCompile}>Compile</Button>
          <Button type="primary" onClick={handleSave}>
            Save
          </Button>
        </Space>
      }
    >
      <Space style={{marginBottom:12}} wrap>
          <span style={{opacity:0.7}}>Insert snippet:</span>
          <Button size="small" onClick={()=>insertSnippet('var s = g.Get<string>("iban") ?? string.Empty;\n')}>Get iban</Button>
          <Button size="small" onClick={()=>insertSnippet('s = System.Text.RegularExpressions.Regex.Replace(s, "[^A-Z0-9]+", "");\n')}>Regex strip non-alnum</Button>
          <Button size="small" onClick={()=>insertSnippet('s = s.ToUpperInvariant();\n')}>Uppercase</Button>
          <Button size="small" onClick={()=>insertSnippet('g.Set("iban", s);\n')}>Set iban</Button>
        </Space>
        <Editor
        height="50vh"
        defaultLanguage="csharp"
        value={code}
        onChange={handleChange}
        onMount={handleMount}
        options={{ minimap: { enabled: false } }}
      />
      <Descriptions
        column={2}
        style={{ marginTop: 12 }}
        items={[
          { key: 'builtin', label: 'Built-in', children: rule.isBuiltin ? 'Yes' : 'No' },
          { key: 'enabled', label: 'Enabled', children: rule.enabled ? 'Yes' : 'No' },
          { key: 'reads', label: 'Reads', children: rule.readsCsv || '-' },
          { key: 'writes', label: 'Writes', children: rule.writesCsv || '-' },
        ]}
      />
    </Card>
  );
}

export function EditorSyncBadge({ saving, synced }: { saving: boolean; synced: boolean }) {
  const status = saving ? 'processing' : synced ? 'success' : 'default';
  const text = saving ? 'saving…' : synced ? 'synced' : 'idle';
  return (
    <Space size={8}>
      <Badge status={status as any} text={text} />
    </Space>
  );
}

