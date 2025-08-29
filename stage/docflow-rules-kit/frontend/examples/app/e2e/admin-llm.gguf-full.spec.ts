import { test, expect } from '@playwright/test'

test.describe('Admin LLM - GGUF Download + Delete', () => {
  test('download job and delete file', async ({ page }) => {
    let job = { id: 'job1', status: 'queued', progress: 0 }
    await page.route('**/api/admin/gguf/available', route => route.fulfill({ json: [] }))
    await page.route('**/api/admin/gguf/download', async route => {
      job = { id: 'job1', status: 'running', progress: 1 }
      route.fulfill({ json: { jobId: job.id } })
    })
    await page.route('**/api/admin/gguf/jobs/*', async route => {
      if (job.progress < 100) { job.progress += 50; if (job.progress >= 100) job = { ...job, progress: 100, status: 'succeeded' } }
      route.fulfill({ json: job })
    })
    await page.route('**/api/admin/gguf/available', route => route.fulfill({ json: [{ name:'dl.gguf', path:'/models/dl.gguf', size: 12_000_000, modified: '2025-08-01T00:00:00Z' }] }))

    await page.goto('/admin/llm')
    await page.getByRole('tab', { name: /GGUF Files/i }).click()

    // Open download modal via "Scarica da HF" from inside the Models tab; if not present in GGUF tab, open via Models tab sequence
    await page.getByRole('tab', { name: /Models/i }).click()
    await page.getByRole('button', { name: /Nuovo modello/i }).click()
    await page.getByRole('button', { name: /Scarica da HF/i }).click()

    await page.getByPlaceholder(/repo/i).fill('user/repo')
    await page.getByPlaceholder(/file/i).fill('dl.gguf')
    await page.getByPlaceholder(/revision/i).fill('main')
    await page.getByRole('button', { name: /Avvia download/i }).click()

    // Switch to GGUF tab and refresh; file should appear after job completes
    await page.getByRole('tab', { name: /GGUF Files/i }).click()
    await page.getByRole('button', { name: /Refresh/i }).click()
    await expect(page.getByText('dl.gguf')).toBeVisible()

    // Delete file
    await page.getByRole('row', { name: /dl\.gguf/ }).getByRole('button', { name: /Elimina/i }).click()
    // confirm dialog button named "Elimina"
    await page.getByRole('button', { name: /^Elimina$/ }).click()
  })
})
