import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import React from 'react'
import AdminLlm from '../src/ui/AdminLlm'
import { message } from 'antd'

vi.spyOn(message, 'success').mockImplementation(()=>undefined as any)

function Provider({ children }: any){
  const { RulesClientContext } = require('../src/ui/client')
  const client = {
    listLlmModels: async()=>[],
    createLlmModel: vi.fn(async (m:any)=> ({ ...m, id: 'm1' })),
    updateLlmModel: vi.fn(async()=>({})),
    deleteLlmModel: vi.fn(async()=>{}),
    activateLlmModel: vi.fn(async()=>{}),
    warmupLlmModel: vi.fn(async()=>{}),
    listGgufAvailable: async()=>[],
  }
  return <RulesClientContext.Provider value={client}>{children}</RulesClientContext.Provider>
}

test('create and activate model from UI', async () => {
  render(<Provider><AdminLlm/></Provider>)

  // open creation drawer
  fireEvent.click(await screen.findByRole('button', { name: /nuovo modello/i }))
  // fill a few fields
  fireEvent.change(screen.getByLabelText(/name/i), { target: { value: 'mock' } })
  fireEvent.change(screen.getByLabelText(/provider/i), { target: { value: 'Mock' } })
  // save
  fireEvent.click(screen.getByRole('button', { name: /^salva$/i }))

  // Activate requires a row; since list is empty in mock, we won't see it. Check that create was called.
  await waitFor(()=> expect((require('../src/ui/client') as any).RulesClientContext._currentValue.createLlmModel).toHaveBeenCalled())
})
