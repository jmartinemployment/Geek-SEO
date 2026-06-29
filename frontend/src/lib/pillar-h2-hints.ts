export function extractPillarH2Hints(html: string | null | undefined): string[] {
  if (!html) {
    return [];
  }

  const hints: string[] = [];
  const h2Regex = /<h2\b([^>]*)>([\s\S]*?)<\/h2>/gi;
  let match: RegExpExecArray | null;

  while ((match = h2Regex.exec(html)) !== null) {
    const attrs = match[1] ?? '';
    const inner = match[2] ?? '';
    const text = inner.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    if (!text || /frequently asked/i.test(text)) {
      break;
    }

    const idMatch = /\bid=["']([^"']+)["']/i.exec(attrs);
    hints.push(idMatch?.[1]?.trim() || text);
  }

  return hints;
}
