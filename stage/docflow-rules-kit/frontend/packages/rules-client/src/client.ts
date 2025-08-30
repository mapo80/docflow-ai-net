import createClient from 'openapi-fetch'
import type { paths } from './schema'

export type { paths } from './schema'

export function createApiClient(baseUrl = '/api', apiKey?: string, getToken?: ()=>string|undefined) {
  return createClient<paths>({ baseUrl, headers: (apiKey || getToken) ? () => {
  const token = getToken?.()
  return token ? { 'Authorization': `Bearer ${token}` } : (apiKey ? { 'X-API-Key': apiKey } : undefined)
} : undefined })
}
