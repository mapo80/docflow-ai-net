import { useEffect, useRef, useState } from 'react'
import { Button, Card, Descriptions, message, Space } from 'antd'
import Editor, { OnMount } from '@monaco-editor/react'
import type { RulesClient, RuleDetail } from '@docflow/rules-client'
import type * as monaco from 'monaco-editor'
import { MonacoLanguageClient, CloseAction, ErrorAction } from 'monaco-languageclient'
import { listen, MessageConnection } from 'vscode-ws-jsonrpc'

let syncTimer: any;
let lastSyncOk = false;
import { Badge, Space } from 'antd'

export function RulesEditor({ client, ruleId, lspUrl, lspApiKey }: { client: RulesClient, ruleId: string, lspUrl?: string, lspApiKey?: string }) {
  const [rule, setRule] = useState<RuleDetail | null>(null)
  const [code, setCode] = useState('')
  const clientRef = useRef<MonacoLanguageClient>()

  const load = async () => {
    const r = await client.getRule(ruleId)
    setRule(r); setCode(r.code)
  }
  useEffect(()=>{ load() }, [ruleId])

  const onSave = async () => {
    if (!rule) return
    await client.updateRule(ruleId, { ...rule, code })
    message.success('Saved'); await load()
  }

  const onCompile = async () => {
    const r = await client.compileRule(ruleId)
    r.ok ? message.success('Compile OK') : message.error(r.errors.join('\n'))
  }

  const handleMount: OnMount = (editor, monaco) => {
    // configure C# model
    const uri = monaco.Uri.parse('inmemory://rules/user.csx')
    let model = monaco.editor.getModel(uri)
    if (!model) model = monaco.editor.createModel(code || '', 'csharp', uri)
    editor.setModel(model)

    if (!lspUrl) return

    const url = new URL(lspUrl.replace(/^http/, 'ws')); url.searchParams.set('workspaceId', ruleId);
    if (lspApiKey) url.searchParams.set('api_key', lspApiKey);
    const [saving, setSaving] = React.useState(false)
  const [synced, setSynced] = React.useState(true)
  const ws = new WebSocket(url.toString())


    async function lspSync(content: string) {
      try {
        const headers = getAuthHeaders ? getAuthHeaders() : {}
        setSaving(true); setSynced(false);
        const res = await fetch(`/lsp/workspace/sync?workspaceId=${encodeURIComponent(ruleId)}`, { method: 'POST', headers: { 'Content-Type': 'application/json', ...(headers as any) }, body: JSON.stringify({ filePath: 'rule.csx', content }) })
        if (res.ok) { setSaving(false); setSynced(true) } else { setSaving(false); }
      } catch {}
    }

    ws.onopen = () => {
      listen({
        webSocket: ws,
        onConnection: (connection: MessageConnection) => {
          const languageClient = new MonacoLanguageClient({
            name: 'C# Language Client',
            clientOptions: {
              documentSelector: [{ language: 'csharp' }],
              errorHandler: { error: () => ErrorAction.Continue, closed: () => CloseAction.Restart }
            },
            connectionProvider: { get: () => Promise.resolve(connection) }
          })
          languageClient.start()
          clientRef.current = languageClient
          connection.onClose(() => languageClient.stop())
        }
      })
    }
  }

  useEffect(()=> () => clientRef.current?.stop(), [])

  if (!rule) return null

  return (
    <Card title={rule.name} extra={<Space><Button onClick={onCompile}>Compile</Button><Button type="primary" onClick={onSave}>Save</Button></Space>}>
      <Editor
        height="50vh"
        defaultLanguage="csharp"
        value={code}
        onChange={v=>setCode(v||'')}
        onMount={handleMount}
        options={{ minimap: { enabled: false } }}
      />
      <Descriptions column={2} style={{ marginTop: 12 }} items={[
        { key: 'builtin', label: 'Built-in', children: rule.isBuiltin ? 'Yes' : 'No' },
        { key: 'enabled', label: 'Enabled', children: rule.enabled ? 'Yes' : 'No' },
        { key: 'reads', label: 'Reads', children: rule.readsCsv || '-' },
        { key: 'writes', label: 'Writes', children: rule.writesCsv || '-' },
      ]} />
    </Card>
  )
}


export function onCodeChangeFactory(ruleId: string, getAuthHeaders?: ()=>HeadersInit) {
  let timer: any
  return (value?: string) => {
    clearTimeout(timer)
    timer = setTimeout(()=>{
      fetch(`/lsp/workspace/sync?workspaceId=${encodeURIComponent(ruleId)}`, { method: 'POST', headers: { 'Content-Type': 'application/json', ...(getAuthHeaders ? getAuthHeaders() : {}) as any }, body: JSON.stringify({ filePath: 'rule.csx', content: value || '' }) })
    }, 500)
  }
}


/* status-badge */
export function EditorSyncBadge({ saving, synced }: { saving: boolean; synced: boolean }) {
  return <Space size={8}><Badge status={saving ? 'processing' : (synced ? 'success' : 'default')} text={saving ? 'saving…' : (synced ? 'synced' : 'idle')} /></Space>
}
