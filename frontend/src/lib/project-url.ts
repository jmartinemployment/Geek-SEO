/** Normalize a user-entered URL or domain for comparison with project.url. */
export function normalizeSiteHost(input: string): string {
  const trimmed = input.trim();
  if (!trimmed) return '';
  try {
    const withScheme = /^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`;
    return new URL(withScheme).hostname.replace(/^www\./i, '').toLowerCase();
  } catch {
    return trimmed.replace(/^www\./i, '').split('/')[0]?.toLowerCase() ?? trimmed.toLowerCase();
  }
}

export function projectHost(projectUrl: string): string {
  return normalizeSiteHost(projectUrl);
}
