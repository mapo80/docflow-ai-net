import { test, expect } from '@playwright/test'

test.describe('Admin LLM - Models CRUD + Activate + Warmup', () => {
  test.beforeEach(async ({ page }) => {
    const models: any[] = []
    await page.route('**/api/admin/llm/models', async route => {
      const req = route.request()
      if (req.method() === 'GET') return route.fulfill({ json: models })
      if (req.method() === 'POST') {
        const body = await req.postDataJSON()
        const m = { ...body, id: 'm'+(models.length+1), createdAt: new Date().toISOString(), updatedAt: new Date().toISOString() }
        models.unshift(m); return route.fulfill({ json: m })
      }
      return route.fallback()
    })
    await page.route('**/api/admin/llm/models/*', async route => {
      const req = route.request()
      const id = req.url().split('/').pop()!
      if (req.method() === 'PUT') {
        const body = await req.postDataJSON()
        const idx = models.findIndex(x => x.id === id); if (idx>=0) models[idx] = { ...models[idx], ...body }
        return route.fulfill({ json: models[idx] })
      }
      if (req.method() === 'DELETE') {
        const idx = models.findIndex(x => x.id === id); if (idx>=0) models.splice(idx,1)
        return route.fulfill({ status: 204 })
      }
      return route.fallback()
    })
    await page.route('**/api/admin/llm/activate', route => route.fulfill({ status: 200 }))
    await page.route('**/api/admin/llm/warmup', route => route.fulfill({ status: 200 }))
    await page.route('**/api/admin/gguf/available', route => route.fulfill({ json: [{ name:'demo.gguf', path:'/models/demo.gguf', size: 10_000_000, modified: '2025-08-01T00:00:00Z' }] }))
  })

  test('create, edit, activate, warmup, delete', async ({ page }) => {
    await page.goto('/admin/llm')

    await page.getByRole('button', { name: /Nuovo modello/i }).click()
    await page.getByLabel('Provider').click()
    await page.getByText('LlamaSharp').click()
    await page.getByLabel('Name').fill('Local GGUF')
    await page.getByLabel('Model GGUF').click()
    await page.getByText(/demo\.gguf/).click()
    await page.getByRole('button', { name: /^Salva$/ }).click()

    // Activate with Turbo ON
    await page.getByRole('switch', { name: /Turbo/i }).click()
    await page.getByRole('row', { name: /Local GGUF/ }).getByRole('button', { name: /Activate/i }).click()

    // Warmup active
    await page.getByRole('button', { name: /Warmup active/i }).click()

    // Edit model (toggle enabled off)
    await page.getByRole('row', { name: /Local GGUF/ }).getByRole('button', { name: /Edit/i }).click()
    await page.getByLabel('Enabled').click()
    await page.getByRole('button', { name: /^Salva$/ }).click()

    // Delete model
    await page.getByRole('row', { name: /Local GGUF/ }).getByRole('button', { name: /Delete/i }).click()
    await page.getByRole('button', { name: /^OK$/ }).click()
  })
})
