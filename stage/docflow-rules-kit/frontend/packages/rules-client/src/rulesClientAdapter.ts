import type { RulesClient, RuleDetail, RuleSummary, TestCase, TestRunResult } from './index'
import { createApiClient } from './client'

export function createRulesClient(baseUrl = '/api', apiKey?: string, getToken?: ()=>string|undefined): RulesClient {
  const api = createApiClient(baseUrl, apiKey, getToken)
  return {
    async listRules(params) {
      const { data, error } = await api.GET('/api/rules', { params: { query: params as any } })
      if (error) return raise(error)
      return data as RuleSummary[]
    },
    async getRule(id: string) {
      const { data, error } = await api.GET('/api/rules/{id}', { params: { path: { id } } })
      if (error) return raise(error)
      return data as RuleDetail
    },
    async createRule(payload) {
      const { data, error } = await api.POST('/api/rules', { body: payload })
      if (error) return raise(error)
      return data as RuleDetail
    },
    async updateRule(id, payload) {
      const { error } = await api.PUT('/api/rules/{id}', { params: { path: { id } }, body: {
        name: payload.name, description: payload.description, code: payload.code,
        readsCsv: payload.readsCsv, writesCsv: payload.writesCsv, enabled: payload.enabled
      } as any })
      if (error) return raise(error)
    },
    async compileRule(id) {
      const { data, error } = await api.POST('/api/rules/{id}/compile', { params: { path: { id } } })
      if (error) return raise(error)
      return data as { ok: boolean, errors: string[] }
    },
    async runRule(id, input) {
      const { data, error } = await api.POST('/api/rules/{id}/run', { params: { path: { id } }, body: { input } })
      if (error) return raise(error)
      return data as any
    },
    async listTests(ruleId) {
      const { data, error } = await api.GET('/api/rules/{ruleId}/tests', { params: { path: { ruleId } } })
      if (error) return raise(error)
      return data as TestCase[]
    },
    async addTest(ruleId, test) {
      const { data, error } = await api.POST('/api/rules/{ruleId}/tests', { params: { path: { ruleId } }, body: test })
      if (error) return raise(error)
      return data as TestCase
    },
    async runTests(ruleId, opts) {
      const { data, error } = await api.POST('/api/rules/{ruleId}/tests/run', { params: { path: { ruleId } }, body: { maxParallelism: opts?.maxParallelism } as any })
      if (error) return raise(error)
      return data as TestRunResult[]
    }
  }
}

,
    async runSelectedTests(ruleId, ids, opts) {
      const { data, error } = await api.POST('/api/rules/{ruleId}/tests/run-selected', { params: { path: { ruleId } }, body: { ids, maxParallelism: opts?.maxParallelism } as any })
      if (error) return raise(error)
      return data as TestRunResult[]
    }

,
    async updateTest(ruleId, testId, meta) {
      const { error } = await api.PUT('/api/rules/{ruleId}/tests/{testId}', { params: { path: { ruleId, testId } }, body: meta as any })
      if (error) return raise(error)
    }

,
    async listSuites(){ const { data, error } = await api.GET('/api/suites'); if (error) return raise(error); return data as any; },
    async createSuite(p){ const { data, error } = await api.POST('/api/suites', { body: p as any }); if (error) return raise(error); return data as any; },
    async updateSuite(id, p){ const { error } = await api.PUT('/api/suites/{id}', { params: { path: { id } }, body: p as any }); if (error) return raise(error); },
    async deleteSuite(id){ const { error } = await api.DELETE('/api/suites/{id}', { params: { path: { id } } }); if (error) return raise(error); },

    async listTags(){ const { data, error } = await api.GET('/api/tags'); if (error) return raise(error); return data as any; },
    async createTag(p){ const { data, error } = await api.POST('/api/tags', { body: p as any }); if (error) return raise(error); return data as any; },
    async updateTag(id, p){ const { error } = await api.PUT('/api/tags/{id}', { params: { path: { id } }, body: p as any }); if (error) return raise(error); },
    async deleteTag(id){ const { error } = await api.DELETE('/api/tags/{id}', { params: { path: { id } } }); if (error) return raise(error); },

    async getCoverage(ruleId){ const { data, error } = await api.GET('/api/rules/{ruleId}/tests/coverage', { params: { path: { ruleId } } }); if (error) return raise(error); return data as any; }


  async suggestTests(ruleId, req) {
    const { data, error } = await api.POST('/api/ai/tests/suggest', { params: { query: { ruleId } }, body: { userPrompt: req?.userPrompt, budget: req?.budget, temperature: req?.temperature, modelId: req?.modelId, turbo: req?.turbo } as any })
    if (error) return raise(error)
    return { suggestions: data?.suggestions as any, model: data?.model as any, totalSkeletons: data?.totalSkeletons as any, usage: { inputTokens: data?.inputTokens as any, outputTokens: data?.outputTokens as any, durationMs: data?.durationMs as any, costUsd: data?.costUsd as any } }
  },
  async importSuggestedTests(ruleId, ids, opts) {
    const { data, error } = await api.POST('/api/ai/tests/import', { params: { query: { ruleId } }, body: { ids, suite: opts?.suite, tags: opts?.tags } as any })
    if (error) return raise(error)
    return data as any
  },


  async listLlmModels() {
    const { data, error } = await api.GET('/api/admin/llm/models', {})
    if (error) return raise(error); return data as any[]
  },
  async createLlmModel(m) {
    const { data, error } = await api.POST('/api/admin/llm/models', { body: m as any })
    if (error) return raise(error); return data as any
  },
  async updateLlmModel(id, m) {
    const { data, error } = await api.PUT('/api/admin/llm/models/{id}', { params: { path: { id } }, body: m as any })
    if (error) return raise(error); return data as any
  },
  async deleteLlmModel(id) {
    const { error } = await api.DELETE('/api/admin/llm/models/{id}', { params: { path: { id } } })
    if (error) return raise(error)
  },
  async activateLlmModel(modelId, turbo) {
    const { error } = await api.POST('/api/admin/llm/activate', { body: { modelId, turbo } as any })
    if (error) return raise(error)
  },
  async warmupLlmModel(modelId) {
    const { error } = await api.POST('/api/admin/llm/warmup', { body: { modelId } as any })
    if (error) return raise(error)
  },


  async listGgufAvailable() {
    const { data, error } = await api.GET('/api/admin/gguf/available', {})
    if (error) return raise(error)
    return data as any[]
  },
  async startGgufDownload(repo, file, revision) {
    const { data, error } = await api.POST('/api/admin/gguf/download', { body: { repo, file, revision } as any })
    if (error) return raise(error)
    return data as any
  },
  async getGgufJob(jobId) {
    const { data, error } = await api.GET('/api/admin/gguf/jobs/{id}', { params: { path: { id: jobId } } })
    if (error) return raise(error)
    return data as any
  },


  async deleteGgufAvailable(path) {
    const { error } = await api.DELETE('/api/admin/gguf/available', { body: { path } as any })
    if (error) return raise(error)
  },


  async cloneRule(id, newName?, includeTests?) {
    const { data, error } = await api.POST('/api/rules/{id}/clone', { params: { path: { id } }, body: { newName, includeTests } })
    if (error) return raise(error)
    return data as any
  },

  async cloneTest(ruleId, testId, opts) {
    const { data, error } = await api.POST('/api/rules/{ruleId}/tests/{testId}/clone', { params: { path: { ruleId, testId } }, body: opts as any })
    if (error) return raise(error)
    return data as any
  },

  async cloneSuite(id, newName) {
    const { data, error } = await api.POST('/api/suites/{id}/clone', { params: { path: { id } }, body: { newName } })
    if (error) return raise(error)
    return data as any
  },

  async fuzzPreview(ruleId, maxPerField) {
    const { data, error } = await api.POST('/api/rules/{ruleId}/fuzz/preview', { params: { path: { ruleId }, query: { maxPerField } } as any })
    if (error) return raise(error)
    return data as any
  },

  async fuzzImport(ruleId, items, suite?, tags?) {
    const { data, error } = await api.POST('/api/rules/{ruleId}/fuzz/import', { params: { path: { ruleId } }, body: { items, suite, tags } })
    if (error) return raise(error)
    return data as any
  },


  async runProperties(ruleId, trials?, seed?) {
    const { data, error } = await api.POST('/api/rules/{ruleId}/properties/run', { params: { path: { ruleId }, query: { trials, seed } as any } })
    if (error) return raise(error)
    return data as any
  },

  async importPropertyFailures(ruleId, failures, suite?, tags?) {
    const { data, error } = await api.POST('/api/rules/{ruleId}/properties/importFailures', { params: { path: { ruleId } }, body: { failures, suite, tags } })
    if (error) return raise(error)
    return data as any
  },
