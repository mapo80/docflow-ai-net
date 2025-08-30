import { render } from '@testing-library/react'
import { EditorSyncBadge } from '../src/RulesEditor'

it('shows saving and synced badges', async () => {
  const { container, rerender } = render(<EditorSyncBadge saving={true} synced={false} />)
  expect(container.textContent?.toLowerCase()).toContain('saving')
  rerender(<EditorSyncBadge saving={false} synced={true} />)
  expect(container.textContent?.toLowerCase()).toContain('synced')
})
