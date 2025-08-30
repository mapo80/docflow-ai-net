import { LspService } from '../generated/services/LspService';

export function syncWorkspace(ruleId: string, content: string) {
  return LspService.postApiV1LspWorkspaceSync({
    workspaceId: ruleId,
    requestBody: { filePath: 'rule.csx', content },
  });
}
