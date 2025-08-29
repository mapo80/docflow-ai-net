
import { Layout, Menu } from 'antd'
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom'
import { AuthProvider, RequireRole } from './Auth'
import AdminLlm from './AdminLlm'

export default function App(){
  return (
    <Layout style={{ minHeight:'100vh' }}>
      <Layout.Header>
        <Menu theme="dark" mode="horizontal" items={[
          { key:'home', label:<Link to='/'>Home</Link> },
          { key:'admin-llm', label:<Link to='/admin/llm'>LLM</Link> },
        ]} />
      </Layout.Header>
      <Layout.Content style={{ padding:24 }}>
        <AuthProvider>
          <BrowserRouter>
            <Routes>
              <Route path='/' element={<div>Welcome — go to <Link to='/admin/llm'>Admin → LLM</Link></div>} />
              <Route path='/admin/llm' element={<RequireRole role='admin'><AdminLlm/></RequireRole>} />
              <Route path="/rules" element={<RulesPage/>} />
          <Route path="/rules/:id" element={<RuleEdit/>} />
          <Route path="/builder" element={<RuleBuilder/>} />
        </Routes>
          </BrowserRouter>
        </AuthProvider>
      </Layout.Content>
    </Layout>
  )
}
