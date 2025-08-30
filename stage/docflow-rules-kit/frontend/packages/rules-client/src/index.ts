export type RuleSummary = {
  id: string; name: string; version: string; isBuiltin: boolean; enabled: boolean; description?: string; updatedAt: string;
}
export type RuleDetail = RuleSummary & { code: string; readsCsv?: string; writesCsv?: string }
export type TestCase = { id: string; name: string; inputJson: string; expectJson: string; updatedAt: string; suite?: string; tags?: string[]; priority?: number }
export type TestRunResult = { id?: string; name: string; passed: boolean; durationMs: number; diff?: any[]; logs: string[]; error?: string; actual?: any }

export interface RulesClient {
  listRules(params?: { search?: string; sortBy?: string; sortDir?: 'asc'|'desc'; page?: number; pageSize?: number }): Promise<{ total: number; page: number; pageSize: number; items: RuleSummary[] }>
  getRule(id: string): Promise<RuleDetail>
  createRule(payload: { name: string; description?: string; code: string; readsCsv?: string; writesCsv?: string; enabled: boolean }): Promise<RuleDetail>
  updateRule(id: string, payload: Partial<RuleDetail>): Promise<void>
  compileRule(id: string): Promise<{ ok: boolean; errors: string[] }>
  runRule(id: string, input: any): Promise<{ before: any; after: any; mutations: any[]; durationMs: number; logs: string[] }>
  listTests(ruleId: string): Promise<TestCase[]>
  addTest(ruleId: string, test: { name: string; input: any; expect: any }): Promise<TestCase>
  runTests(ruleId: string): Promise<TestRunResult[]>
}

export { createApiClient } from './client'
export { createRulesClient } from './rulesClientAdapter'


export type Suite = { id: string; name: string; color?: string; description?: string; updatedAt: string }
export type Tag = { id: string; name: string; color?: string; description?: string; updatedAt: string }

export interface RulesClient {
  listSuites(): Promise<Suite[]>;
  createSuite(p: { name: string; color?: string; description?: string }): Promise<Suite>;
  updateSuite(id: string, p: { name: string; color?: string; description?: string }): Promise<void>;
  deleteSuite(id: string): Promise<void>;
  cloneSuite(id: string, newName: string): Promise<{ id: string; name: string }>;

  listTags(): Promise<Tag[]>;
  createTag(p: { name: string; color?: string; description?: string }): Promise<Tag>;
  updateTag(id: string, p: { name: string; color?: string; description?: string }): Promise<void>;
  deleteTag(id: string): Promise<void>;

  getCoverage(ruleId: string): Promise<Array<{ field: string; tested: number; mutated: number; hits: number; pass: number }>>;
}
