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
