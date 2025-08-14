import { describe, it, expect, beforeEach } from 'vitest';
import { loadPresets, savePreset, deletePreset, renamePreset, type ModelPreset } from './presetStore';

describe('presetStore', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('saves and loads presets without token', () => {
    const p: ModelPreset = {
      name: 'test',
      repo: 'repo',
      file: 'file',
      contextSize: 1024,
      updatedAt: new Date().toISOString(),
    };
    savePreset(p);
    const list = loadPresets();
    expect(list[0]).toEqual(p);
  });

  it('renames preset', () => {
    const p: ModelPreset = {
      name: 'old',
      repo: 'r',
      file: 'f',
      contextSize: 1,
      updatedAt: new Date().toISOString(),
    };
    savePreset(p);
    renamePreset('old', 'new');
    const list = loadPresets();
    expect(list[0].name).toBe('new');
  });

  it('deletes preset', () => {
    const p: ModelPreset = {
      name: 'a',
      repo: 'r',
      file: 'f',
      contextSize: 1,
      updatedAt: new Date().toISOString(),
    };
    savePreset(p);
    deletePreset('a');
    expect(loadPresets()).toHaveLength(0);
  });
});
