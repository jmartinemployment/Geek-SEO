const URL_LIKE = /^(?:https?:\/\/)?(?:[\da-z.-]+)\.(?:[a-z.]{2,})(?:[/\w .-]*)*$/iu;

export function normalizeWebsiteUrl(raw: string): string | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;

  const candidate = /^https?:\/\//iu.test(trimmed) ? trimmed : `https://${trimmed}`;

  try {
    const parsed = new URL(candidate);
    if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') return null;
    if (!parsed.hostname.includes('.')) return null;
    if (parsed.hostname.includes('@')) return null;

    const path = parsed.pathname === '/' ? '' : parsed.pathname;
    return `${parsed.protocol}//${parsed.host}${path}${parsed.search}`.replace(/\/$/u, '') || `${parsed.protocol}//${parsed.host}`;
  } catch {
    return null;
  }
}

export function isLikelyWebsiteUrl(raw: string): boolean {
  const trimmed = raw.trim();
  if (!trimmed || trimmed.includes(' ')) return false;
  return URL_LIKE.test(trimmed);
}

export function websiteUrlError(raw: string): string | null {
  const trimmed = raw.trim();
  if (!trimmed) return 'Enter your website URL.';
  if (trimmed.includes(' ')) return 'Enter a website URL (for example, geekatyourspot.com).';
  if (!isLikelyWebsiteUrl(trimmed)) return 'Enter a valid website URL (for example, geekatyourspot.com).';
  if (normalizeWebsiteUrl(trimmed) === null) return 'That does not look like a website URL.';
  return null;
}
