const PLACEHOLDER_H1 = /^article title$/i;

export function stripHtmlTags(value: string): string {
  return value
    .replace(/<[^>]+>/g, '')
    .replace(/&nbsp;/gi, ' ')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .trim();
}

export function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export function extractH1Text(html: string): string | null {
  const match = html.match(/<h1\b[^>]*>([\s\S]*?)<\/h1>/i);
  if (!match) return null;
  const text = stripHtmlTags(match[1]);
  return text || null;
}

export function isPlaceholderH1(text: string | null | undefined): boolean {
  if (!text) return true;
  return PLACEHOLDER_H1.test(text.trim());
}

export function replaceOrPrependH1(html: string, title: string): string {
  const headline = title.trim() || 'Untitled article';
  const h1 = `<h1>${escapeHtml(headline)}</h1>`;
  if (/<h1\b[^>]*>/i.test(html)) {
    return html.replace(/<h1\b[^>]*>[\s\S]*?<\/h1>/i, h1);
  }
  const body = html.trim();
  return body ? `${h1}\n${body}` : h1;
}

export function draftHtmlFromTitle(title: string): string {
  const headline = title.trim() || 'Untitled article';
  return `${replaceOrPrependH1('', headline)}\n<p>Start writing your article.</p>`;
}

/** Align body H1 with the article title when missing or still the default placeholder. */
export function normalizeDraftHtml(contentHtml: string, title: string): string {
  const trimmed = contentHtml.trim();
  const headline = title.trim() || 'Untitled article';
  if (!trimmed) return draftHtmlFromTitle(headline);

  const h1 = extractH1Text(trimmed);
  if (!h1 || isPlaceholderH1(h1)) {
    return replaceOrPrependH1(trimmed, headline);
  }

  return trimmed;
}

export function titleFromHtml(html: string, fallback: string): string {
  const fromH1 = extractH1Text(html);
  if (fromH1 && !isPlaceholderH1(fromH1)) return fromH1;
  const trimmed = fallback.trim();
  return trimmed || 'Untitled article';
}
