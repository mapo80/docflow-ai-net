
import { test, expect } from '@playwright/test'

test('Admin LLM full flow: create, activate, gguf delete/download, edit model', async ({ page }) => {
  type Model = { id: string, provider: string, name: string, modelPathOrId?: string, enabled?: boolean }
  const models: Model[] = []
  let activeModelId: string | null = null
  let warmupCalled = false
  let jobPolls = 0
  let ggufs = [{ name:'qwen-old.gguf', path:'/models/qwen-old.gguf', size: 123000000, modified: new Date().toISOString() }]

  await page.route('**/api/admin/llm/models', async route => {
    if (route.request().method() === 'GET') {
      return route.fulfill({ json: models })
    }
    if (route.request().method() === 'POST') {
      const body = await route.request().postDataJSON()
      const m: Model = { id: 'm1', provider: body.provider, name: body.name, modelPathOrId: body.modelPathOrId, enabled: body.enabled }
      models.push(m)
      return route.fulfill({ json: m })
    }
    return route.continue()
  })

  await page.route('**/api/admin/llm/models/*', async route => {
    const method = route.request().method()
    const url = new URL(route.request().url())
    const id = url.pathname.split('/').pop()!
    if (method === 'PUT') {
      const body = await route.request().postDataJSON()
      const idx = models.findIndex(m => m.id === id)
      if (idx >= 0) models[idx] = { ...models[idx], ...body }
      return route.fulfill({ json: models[idx] })
    }
    if (method === 'DELETE') {
      const idx = models.findIndex(m => m.id === id)
      if (idx >= 0) models.splice(idx,1)
      return route.fulfill({ status: 204 })
    }
    return route.continue()
  })

  await page.route('**/api/admin/llm/activate', route => {
    if (route.request().method() === 'POST') {
      return route.request().postDataJSON().then((body:any)=>{ activeModelId = body.modelId; return route.fulfill({ status: 200 }) })
    }
    return route.continue()
  })

  await page.route('**/api/admin/llm/warmup', route => {
    if (route.request().method() === 'POST') { warmupCalled = true; return route.fulfill({ status: 200 }) }
    return route.continue()
  })

  // GGUF endpoints
  await page.route('**/api/admin/gguf/available', route => {
    if (route.request().method() === 'GET') return route.fulfill({ json: ggufs })
    if (route.request().method() === 'DELETE') { 
      const body = route.request().postDataJSON && (route.request() as any).postDataJSON()
      const path = (body && (body as any).path) || ''
      ggufs = ggufs.filter(g => g.path !== path) 
      return route.fulfill({ status: 204 })
    }
    return route.continue()
  })

  await page.route('**/api/admin/gguf/download', route => route.fulfill({ json: { jobId: 'job1' } }))
  await page.route('**/api/admin/gguf/jobs/*', route => {
    jobPolls++
    if (jobPolls < 3) return route.fulfill({ json: { id:'job1', status:'running', progress: jobPolls*30 } })
    // success and add file
    const f = { name:'qwen-new.gguf', path:'/models/qwen-new.gguf', size: 125000000, modified: new Date().toISOString() }
    if (!ggufs.find(x => x.path === f.path)) ggufs.push(f)
    return route.fulfill({ json: { id:'job1', status:'succeeded', progress: 100, filePath: f.path } })
  })

  // Go to page
  await page.goto('/admin/llm')
  await expect(page.getByText('LLM Admin')).toBeVisible()

  // Create model
  await page.getByRole('button', { name: /Nuovo modello/i }).click()
  await page.getByLabel('Name').fill('Local Qwen')
  // Select GGUF
  await page.getByLabel('Model GGUF').click()
  await page.getByText('qwen-old.gguf').click()
  // Save
  await page.getByRole('button', { name: /^Salva$/ }).click()
  await expect(page.getByText('Local Qwen')).toBeVisible()

  // Activate with Turbo
  await page.getByRole('switch', { name: /Turbo/i }).check()
  await page.getByRole('button', { name: /^Activate$/ }).click()
  expect(activeModelId).toBe('m1')

  // Warmup active
  await page.getByRole('button', { name: /Warmup active/i }).click()
  expect(warmupCalled).toBeTruthy()

  // Switch to GGUF tab and delete old file
  await page.getByRole('tab', { name: /GGUF Files/i }).click()
  await expect(page.getByText('qwen-old.gguf')).toBeVisible()
  await page.getByRole('button', { name: /^Elimina$/ }).click()
  await page.getByRole('button', { name: /^Elimina$/ }).click() // confirm
  await expect(page.getByText('qwen-old.gguf')).toHaveCount(0)

  // Download new file
  await page.getByRole('button', { name: /Refresh/ }).click() // ensure list refresh
  await page.getByRole('tab', { name: /Models/i }).click()
  await page.getByRole('button', { name: /Nuovo modello/i }).click()
  await page.getByRole('button', { name: /Scarica da HF/i }).click()
  await page.getByLabel('repo').fill('owner/repo')
  await page.getByLabel('file').fill('qwen-new.gguf')
  await page.getByLabel('revision').fill('main')
  await page.getByRole('button', { name: /Avvia download/i }).click()
  await expect(page.getByText(/Download completato/i)).toBeVisible({ timeout: 5000 })

  // Use new file from dropdown
  await page.getByLabel('Model GGUF').click()
  await page.getByText('qwen-new.gguf').click()
  await page.getByRole('button', { name: /^Salva$/ }).click()
  await expect(page.getByText('Local Qwen')).toBeVisible()
})
