import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import React from 'react'
import AdminLlm from '../src/ui/AdminLlm'
import { message, Modal } from 'antd'

vi.spyOn(message, 'success').mockImplementation(()=>undefined as any)
vi.spyOn(message, 'error').mockImplementation(()=>undefined as any)

vi.spyOn(Modal, 'confirm').mockImplementation(({ onOk }: any)=>{ onOk && onOk(); return { destroy(){}} as any })

function Provider({ children }: any){
  // minimal client provider polyfill
  const Ctx = (require('../src/ui/client') as any).RulesClientContext
  const client = {
    listLlmModels: async()=>[],
    listGgufAvailable: async()=>[{ name:'a.gguf', path:'/models/a.gguf', size: 123000000, modified: '2025-08-01T00:00:00Z' }],
    deleteGgufAvailable: async(path:string)=>{},
    startGgufDownload: async()=>({ jobId: '1' }),
    getGgufJob: async()=>({ id:'1', status:'succeeded', progress:100 }),
    activateLlmModel: async()=>{},
    warmupLlmModel: async()=>{},
  }
  return <Ctx.Provider value={client}>{children}</Ctx.Provider>
}

test('GGUF tab renders and delete works', async() => {
  render(<Provider><AdminLlm/></Provider>)
  // switch to GGUF tab
  const tab = await screen.findByRole('tab', { name: /gguf files/i })
  fireEvent.click(tab)
  await screen.findByText('a.gguf')
  const btn = await screen.findByRole('button', { name: /elimina/i })
  fireEvent.click(btn)
  await waitFor(()=> expect(message.success).toHaveBeenCalled())
})
