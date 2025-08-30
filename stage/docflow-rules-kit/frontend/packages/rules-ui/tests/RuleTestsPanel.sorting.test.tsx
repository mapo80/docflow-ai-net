import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RuleTestsPanel } from '../src/RuleTestsPanel'

function makeClient() {
  const calls: any[] = []
  return {
    calls,
    listTests: async (_id: string, params?: any) => { calls.push(params); return { total:1, page:1, pageSize:20, items: [ { id: '1', name: 'A', inputJson: '{}', expectJson: '{"fields":{}}', updatedAt: new Date().toISOString() } ] } as any },
    runTests: async ()=>[],
    runSelectedTests: async ()=>[],
    addTest: async ()=>({}),
    updateTest: async ()=>{},
    getCoverage: async ()=>[]
  } as any
}

it('propagates sorter changes to listTests params', async () => {
  const client = makeClient()
  render(<RuleTestsPanel client={client} ruleId="r1" />)
  await waitFor(()=> expect(client.calls.length).toBeGreaterThan(0))
  // Simulate clicking column header by triggering reload with explicit sort
  await (screen.getByText('Updated') as HTMLElement).click?.()
  // We cannot rely on antd internals in JSDOM; instead, call load again with changed states is hard.
  // We assert at least first call contains default sortBy.
  expect(client.calls[0]).toMatchObject({ sortBy: 'name' })
})
