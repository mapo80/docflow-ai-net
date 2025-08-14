export type ModelPreset = {
  name: string;
  repo: string;
  file: string;
  contextSize: number;
  updatedAt: string;
};

const KEY = 'model-presets';

export function loadPresets(): ModelPreset[] {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return [];
    return JSON.parse(raw) as ModelPreset[];
  } catch {
    return [];
  }
}

function persist(presets: ModelPreset[]) {
  localStorage.setItem(KEY, JSON.stringify(presets));
}

export function savePreset(preset: ModelPreset) {
  const list = loadPresets();
  const idx = list.findIndex((p) => p.name === preset.name);
  if (idx >= 0) list[idx] = preset;
  else list.push(preset);
  persist(list);
}

export function deletePreset(name: string) {
  const list = loadPresets().filter((p) => p.name !== name);
  persist(list);
}

export function renamePreset(oldName: string, newName: string) {
  const list = loadPresets();
  const idx = list.findIndex((p) => p.name === oldName);
  if (idx >= 0) {
    list[idx].name = newName;
    list[idx].updatedAt = new Date().toISOString();
    persist(list);
  }
}
