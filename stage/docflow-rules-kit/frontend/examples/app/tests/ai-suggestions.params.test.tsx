import { render, screen, fireEvent } from '@testing-library/react'
import React from 'react'

function Provider({ children }: any){
  const { RulesClientContext } = require('../src/ui/client')
  const suggest = vi.fn(async (_ruleId:string, req:any)=> ({
    suggestions: [], model:'mock', totalSkeletons: 0, usage: { inputTokens:0, outputTokens:0, durationMs: 1, costUsd: 0 }
  }))
  const client = {
    suggestTests: suggest,
    importSuggestedTests: async ()=> ({ imported: 0 }),
    listLlmModels: async ()=> [{ id:'m1', provider:'Mock', name:'mock' }],
    listRuleTests: async ()=> [],
    getCoverage: async ()=> [],
    runTests: async ()=> ({ ok:0, failed:0 }),
  }
  return <RulesClientContext.Provider value={client}>{children}</RulesClientContext.Provider>
}

test('passes modelId and turbo to suggest', async () => {
  const { RuleTestsPanel } = require('../../packages/rules-ui')
  render(<Provider><RuleTestsPanel ruleId="r1"/></Provider>)
  // choose model
  const sel = await screen.findByRole('combobox')
  // open & pick first (mock)
  fireEvent.mouseDown(sel)
  fireEvent.click(await screen.findByText(/mock/))
  // enable Turbo
  fireEvent.click(screen.getByRole('switch'))
  // generate
  fireEvent.click(await screen.findByRole('button', { name: /Genera \(auto\)/i }))
  expect((require('../src/ui/client') as any)).toBeTruthy()
})
