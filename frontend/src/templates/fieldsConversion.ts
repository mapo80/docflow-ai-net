export interface SimpleField {
  key: string;
  value: string;
}

export const slugRegex = /^[A-Za-z0-9_-]+$/;

export function isSlug(token: string): boolean {
  return slugRegex.test(token);
}

export function slugify(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9_-]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .substring(0, 100);
}

function parseValue(v: string): any {
  try {
    return JSON.parse(v);
  } catch {
    return v;
  }
}

export function simpleToObject(fields: SimpleField[]): Record<string, any> {
  const result: Record<string, any> = {};
  for (const f of fields) {
    if (!f.key) continue;
    const value = parseValue(f.value);
    if (Object.prototype.hasOwnProperty.call(result, f.key)) {
      const current = result[f.key];
      if (Array.isArray(current)) {
        current.push(value);
      } else {
        result[f.key] = [current, value];
      }
    } else {
      result[f.key] = value;
    }
  }
  return result;
}

export function objectToSimple(obj: Record<string, any>): SimpleField[] {
  const fields: SimpleField[] = [];
  for (const key of Object.keys(obj)) {
    const val = obj[key];
    if (Array.isArray(val)) {
      for (const item of val) {
        fields.push({
          key,
          value: typeof item === 'object' ? JSON.stringify(item) : String(item),
        });
      }
    } else if (val !== null && typeof val === 'object') {
      fields.push({ key, value: JSON.stringify(val) });
    } else {
      fields.push({ key, value: String(val) });
    }
  }
  return fields;
}

export function simpleToJsonText(fields: SimpleField[]): string {
  return JSON.stringify(simpleToObject(fields), null, 2);
}

export function jsonTextToSimple(text: string): SimpleField[] {
  const parsed = JSON.parse(text);
  if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
    throw new Error('Root must be object');
  }
  return objectToSimple(parsed as Record<string, any>);
}
