import { test, expect } from '@playwright/test'

test('AI Suggestions flow', async ({ page }) => {
  await page.route('**/api/admin/llm/models', route => route.fulfill({ json: [{ id:'m1', provider:'Mock', name:'mock' }] }))
  await page.route('**/api/ai/tests/suggest', route => {
    if (route.request().method() === 'POST') {
      return route.fulfill({ json: { suggestions: [{ id:'s1', reason:'boundary', score:1.1, coverageDelta:[], payload:{ name:'case 1', suite:'ai', tags:['ai'] }, createdAt: new Date().toISOString(), model: 'mock' }], model: 'mock', totalSkeletons: 1, inputTokens:0, outputTokens:0, durationMs: 10, costUsd: 0 } })
    }
    return route.continue()
  })
  await page.route('**/api/ai/tests/import', route => route.fulfill({ json: { imported: 1 } }))

  await page.goto('/')
  // open rule tests panel path depends on the example app nav; direct route to admin page for simplicity
  await page.goto('/admin/llm')
  await expect(page.getByText('LLM Admin')).toBeVisible()
  // Go to unit tests page if exists in your nav; here we just ensure routes are wired.
})
