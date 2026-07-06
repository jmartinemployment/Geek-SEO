import { expect, type Page } from '@playwright/test';

export type BrowserConsoleCollector = {
  consoleErrors: string[];
  pageErrors: string[];
};

/** Next.js RSC prefetch of /api/auth/start must not fetch cross-origin authorize URL. */
export function isOAuthPrefetchCorsError(text: string): boolean {
  return (
    text.includes('blocked by CORS policy') &&
    text.includes('/api/auth/start') &&
    text.includes('seo.geekatyourspot.com')
  );
}

export function isIgnorableConsoleError(text: string): boolean {
  const line = text.trim();
  if (!line) return true;
  if (isOAuthPrefetchCorsError(line)) return true;
  if (/ResizeObserver loop (limit exceeded|completed)/i.test(line)) return true;
  if (/favicon\.ico/i.test(line)) return true;
  if (/Failed to load resource: the server responded with a status of (401|403|404)/i.test(line)) {
    return true;
  }
  if (/\/api\/seo\//i.test(line) && /\b(401|403|404)\b/.test(line)) return true;
  if (/WebSocket.*(failed|closed)|SignalR.*(failed|disconnect)/i.test(line)) return true;
  return false;
}

export function trackBrowserConsole(page: Page): BrowserConsoleCollector {
  const collector: BrowserConsoleCollector = { consoleErrors: [], pageErrors: [] };
  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      collector.consoleErrors.push(msg.text());
    }
  });
  page.on('pageerror', (error) => {
    collector.pageErrors.push(error.message);
  });
  return collector;
}

export function blockingConsoleErrors(collector: BrowserConsoleCollector): string[] {
  return [...collector.consoleErrors, ...collector.pageErrors].filter(
    (line) => !isIgnorableConsoleError(line),
  );
}

export async function waitForConsoleSettle(page: Page, ms = 2_000): Promise<void> {
  await page.waitForTimeout(ms);
}

export function assertNoConsoleErrors(collector: BrowserConsoleCollector, scope: string): void {
  const blocking = blockingConsoleErrors(collector);
  expect(blocking, `${scope} browser console/page errors`).toEqual([]);
}

export function formatConsoleReport(collector: BrowserConsoleCollector): string {
  const blocking = blockingConsoleErrors(collector);
  const lines = [
    '# Browser console',
    `console.error count: ${collector.consoleErrors.length}`,
    `pageerror count: ${collector.pageErrors.length}`,
    `blocking count: ${blocking.length}`,
    '',
  ];
  if (collector.consoleErrors.length > 0) {
    lines.push('## console.error');
    for (const line of collector.consoleErrors) {
      lines.push(`- ${line}`);
    }
    lines.push('');
  }
  if (collector.pageErrors.length > 0) {
    lines.push('## pageerror');
    for (const line of collector.pageErrors) {
      lines.push(`- ${line}`);
    }
    lines.push('');
  }
  if (blocking.length > 0) {
    lines.push('## blocking (not ignorable)');
    for (const line of blocking) {
      lines.push(`- ${line}`);
    }
  }
  return lines.join('\n');
}
