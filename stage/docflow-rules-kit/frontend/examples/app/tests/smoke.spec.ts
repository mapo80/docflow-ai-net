import { test, expect } from '@playwright/test'

test('app loads and shows menu', async ({ page }) => {
  await page.route('**/api/**', route => {
    const url = route.request().url()
    if (url.endsWith('/api/rules')) return route.fulfill({ json: { total: 1, page: 1, pageSize: 50, items: [ { id: 'r1', name: 'Rule 1', isBuiltin: false, version: '0.1.0', updatedAt: new Date().toISOString() } ] } })
    if (url.includes('/tests')) return route.fulfill({ json: { total: 0, page: 1, pageSize: 20, items: [] } })
    if (url.endsWith('/api/suites')) return route.fulfill({ json: [] })
    if (url.endsWith('/api/tags')) return route.fulfill({ json: [] })
    return route.continue()
  })
  await page.addInitScript(()=>{ localStorage.setItem('FAKE_BEARER','e2e-token') });
  await page.goto('/')
  await expect(page.getByText('Docflow Rules (Example)')).toBeVisible()
  await page.getByRole('link', {name: 'Home'}).click()
  await expect(page.getByText('Rule 1')).toBeVisible()
})
