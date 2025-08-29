import { render, screen, fireEvent } from '@testing-library/react'
import React from 'react'
import { RuleTestsPanel } from '../../packages/rules-ui'

test('suggest uses modelId and turbo', async () => {
  const { RulesClientContext } = require('../src/ui/client')
  const suggest = vi.fn(async ()=> ({
    suggestions: [], model:'mock', totalSkeletons: 0, usage: { inputTokens:0, outputTokens:0, durationMs:0, costUsd:0 }
  }))
  const client = {
    suggestTests: suggest,
    listLlmModels: async()=>[{ id:'m1', provider:'Mock', name:'mock' }],
    listRuleTests: async()=>[], getCoverage: async()=>[], runTests: async()=>({ ok:0, failed:0 }), importSuggestedTests: async()=>({ imported:0 })
  }
  render(<RulesClientContext.Provider value={client as any}><RuleTestsPanel ruleId="r1"/></RulesClientContext.Provider>)
  // Pick model + Turbo and generate
  const modelSel = await screen.findByRole('combobox', { name: '' })
  fireEvent.mouseDown(modelSel)
  const opt = await screen.findByText(/mock/i)
  fireEvent.click(opt)
  const turbo = screen.getByRole('switch')
  fireEvent.click(turbo)
  const gen = screen.getByRole('button', { name: /genera \(auto\)/i })
  fireEvent.click(gen)
  await new Promise(r => setTimeout(r, 10))
  expect(suggest).toHaveBeenCalledWith('r1', expect.objectContaining({ modelId: 'm1', turbo: true }))
})
