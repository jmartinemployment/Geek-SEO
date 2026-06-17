export type SeoGateErrorBody = {
  error?: string;
  requiredTier?: string;
  currentTier?: string;
  feature?: string;
  usage?: number;
  limit?: number;
  upgradeUrl?: string;
};

export class SeoApiError extends Error {
  readonly status: number;
  readonly body: SeoGateErrorBody;

  constructor(status: number, body: SeoGateErrorBody) {
    super(formatSeoApiErrorMessage(status, body));
    this.name = 'SeoApiError';
    this.status = status;
    this.body = body;
  }

  get isUpgradeRequired(): boolean {
    return this.status === 402;
  }

  get isUsageLimit(): boolean {
    return this.status === 429;
  }
}

export function formatSeoApiErrorMessage(status: number, body: SeoGateErrorBody): string {
  if (status === 402 && body.requiredTier) {
    const current = body.currentTier ? ` (current: ${body.currentTier})` : '';
    return body.error ?? `Upgrade to ${body.requiredTier} required${current}.`;
  }
  if (status === 429 && body.feature) {
    const usage =
      body.usage !== undefined && body.limit !== undefined
        ? ` — ${body.usage}/${body.limit} used`
        : '';
    return body.error ?? `Usage limit reached for ${body.feature}${usage}.`;
  }
  return body.error ?? `Request failed (${status})`;
}

function extractValidationMessage(parsed: Record<string, unknown>): string | undefined {
  const errors = parsed.errors;
  if (!errors || typeof errors !== 'object') return undefined;

  const messages: string[] = [];
  for (const value of Object.values(errors as Record<string, unknown>)) {
    if (Array.isArray(value)) {
      for (const item of value) {
        if (typeof item === 'string' && item.trim()) messages.push(item);
      }
    } else if (typeof value === 'string' && value.trim()) {
      messages.push(value);
    }
  }

  return messages.length > 0 ? messages.join(' ') : undefined;
}

export async function parseSeoApiErrorResponse(res: Response): Promise<SeoApiError> {
  let body: SeoGateErrorBody = {};
  const text = await res.text();
  if (text) {
    try {
      const parsed: unknown = JSON.parse(text);
      if (typeof parsed === 'string') {
        body = { error: parsed };
      } else if (parsed && typeof parsed === 'object') {
        const record = parsed as Record<string, unknown>;
        const validation = extractValidationMessage(record);
        const title = typeof record.title === 'string' ? record.title : undefined;
        const explicitError = typeof record.error === 'string' ? record.error : undefined;
        body = {
          ...(parsed as SeoGateErrorBody),
          error: explicitError ?? validation ?? title,
        };
      }
    } catch {
      body = { error: text };
    }
  }
  return new SeoApiError(res.status, body);
}
