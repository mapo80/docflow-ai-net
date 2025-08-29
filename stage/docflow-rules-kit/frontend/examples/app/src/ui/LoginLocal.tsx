import React from 'react'
import { Button, Card, Form, Input, Checkbox, Alert } from 'antd'
import { useNavigate } from 'react-router-dom'
import { useAuth } from './Auth'

export default function LoginLocal() {
  const [loading, setLoading] = React.useState(false)
  const [error, setError] = React.useState<string | null>(null)
  const nav = useNavigate()
  const auth = useAuth()

  async function onFinish(v: any) {
    setLoading(true); setError(null)
    try {
      const res = await fetch('/api/auth/login', { method: 'POST', headers: { 'Content-Type':'application/json', 'X-Auth-Source':'local' }, body: JSON.stringify({ username: v.username, password: v.password }) })
      if (!res.ok) throw new Error((await res.json())?.error || 'login_failed')
      const data = await res.json()
      await auth.loginLocal(data.access_token, v.remember)
      nav('/')
    } catch (e:any) {
      setError(e?.message || 'login_failed')
    } finally { setLoading(false) }
  }

  return <div style={{ display:'grid', placeItems:'center', height:'70vh' }}>
    <Card title="Login (Local)" style={{ width: 360 }}>
      {error && <Alert type="error" showIcon message="Errore" description={error} style={{ marginBottom: 12 }} />}
      <Form layout="vertical" onFinish={onFinish}>
        <Form.Item name="username" label="Username" rules={[{ required:true }]}><Input autoFocus /></Form.Item>
        <Form.Item name="password" label="Password" rules={[{ required:true }]}><Input.Password /></Form.Item>
        <Form.Item name="remember" valuePropName="checked" initialValue={true}><Checkbox>Remember me</Checkbox></Form.Item>
        <Form.Item><Button type="primary" htmlType="submit" loading={loading} block>Login</Button></Form.Item>
      </Form>
    </Card>
  </div>
}
