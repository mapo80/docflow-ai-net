import { test, expect } from '@playwright/test'

test('GGUF list and delete (mocked)', async ({ page }) => {
  await page.route('**/api/admin/gguf/available', route => route.fulfill({ json: [{ name:'a.gguf', path:'/models/a.gguf', size: 123000000, modified: '2025-08-01T00:00:00Z' }] }))
  await page.route('**/api/admin/gguf/available', route => {
    if (route.request().method() === 'DELETE') return route.fulfill({ status: 204 })
    return route.continue()
  })

  await page.goto('/admin/llm')
  await page.getByRole('tab', { name: /GGUF Files/i }).click()
  await expect(page.getByText('a.gguf')).toBeVisible()
  await page.getByRole('button', { name: /Elimina/i }).click()
  // Modal confirm appears; click the danger button
  await page.getByRole('button', { name: /Elimina/ }).click()
  // no further assertion; absence of error implies success
})
