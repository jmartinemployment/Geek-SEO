import { expect, type Locator, type Page } from '@playwright/test';

export type { BrowserConsoleCollector } from './console-helpers';
export {
  assertNoConsoleErrors,
  formatConsoleReport,
  isIgnorableConsoleError,
  isOAuthPrefetchCorsError,
  trackBrowserConsole,
  waitForConsoleSettle,
} from './console-helpers';

export type ButtonAuditEntry = {
  name: string;
  disabled: boolean;
  visible: boolean;
  clicked: boolean;
  outcome: 'visible-only' | 'clicked' | 'skipped-disabled' | 'skipped-action' | 'skipped-nav' | 'error';
  error?: string;
};

export type ButtonAuditReport = {
  scope: string;
  url: string;
  entries: ButtonAuditEntry[];
};

const GLOBAL_NAV_BUTTONS =
  /^(home|create|site|analyze|content|app center|collapse menu|opportunities|clusters)$/i;

export function clickActionsEnabled(): boolean {
  return process.env.BUTTON_AUDIT_CLICK_ACTIONS === 'true';
}

export async function expandToggleButtons(scope: Locator): Promise<void> {
  const showButtons = scope.getByRole('button', { name: /^show$/i });
  const count = await showButtons.count();
  for (let i = 0; i < count; i++) {
    await showButtons.nth(i).click({ timeout: 5_000 }).catch(() => undefined);
  }
}

export async function auditButtons(
  page: Page,
  scope: Locator,
  scopeLabel: string,
  classify: (name: string) => 'safe' | 'action' | 'nav',
): Promise<ButtonAuditReport> {
  const entries: ButtonAuditEntry[] = [];
  const buttons = scope.getByRole('button');
  const count = await buttons.count();
  const clickActions = clickActionsEnabled();

  for (let i = 0; i < count; i++) {
    const button = buttons.nth(i);
    const visible = await button.isVisible({ timeout: 2_000 }).catch(() => false);
    if (!visible) continue;

    const name = (
      (await button.getAttribute('aria-label', { timeout: 2_000 }).catch(() => null)) ??
      (await button.innerText({ timeout: 2_000 }).catch(() => '')) ??
      ''
    )
      .replace(/\s+/g, ' ')
      .trim();
    if (!name) continue;

    const disabled = await button.isDisabled().catch(() => false);
    const kind = classify(name);

    if (kind === 'nav') {
      entries.push({ name, disabled, visible, clicked: false, outcome: 'skipped-nav' });
      continue;
    }

    if (disabled) {
      entries.push({ name, disabled, visible, clicked: false, outcome: 'skipped-disabled' });
      continue;
    }

    const shouldClick = kind === 'safe' || (kind === 'action' && clickActions);
    if (!shouldClick) {
      entries.push({ name, disabled, visible, clicked: false, outcome: 'skipped-action' });
      continue;
    }

    try {
      await button.click({ timeout: 10_000 });
      await page.waitForTimeout(400);
      entries.push({ name, disabled, visible, clicked: true, outcome: 'clicked' });
    } catch (error) {
      entries.push({
        name,
        disabled,
        visible,
        clicked: false,
        outcome: 'error',
        error: error instanceof Error ? error.message : String(error),
      });
    }
  }

  return { scope: scopeLabel, url: page.url(), entries };
}

export function assertButtonAudit(report: ButtonAuditReport) {
  const visibleButtons = report.entries.filter((entry) => entry.visible);
  expect(visibleButtons.length, `${report.scope} should expose buttons`).toBeGreaterThan(0);

  const errors = report.entries.filter((entry) => entry.outcome === 'error');
  expect(errors, `${report.scope} button click errors`).toEqual([]);

  const clicked = report.entries.filter((entry) => entry.clicked);
  const disabledWorkspace = report.entries.filter(
    (entry) => entry.outcome === 'skipped-disabled' && entry.visible,
  );
  if (clicked.length === 0 && disabledWorkspace.length > 0) {
    return;
  }
  expect(clicked.length, `${report.scope} should click at least one safe button`).toBeGreaterThan(0);
}

export function formatAuditReport(report: ButtonAuditReport): string {
  const lines = [`# ${report.scope}`, `URL: ${report.url}`, ''];
  for (const entry of report.entries) {
    lines.push(
      `- ${entry.name} | visible=${entry.visible} disabled=${entry.disabled} outcome=${entry.outcome}${
        entry.error ? ` error=${entry.error}` : ''
      }`,
    );
  }
  return lines.join('\n');
}
