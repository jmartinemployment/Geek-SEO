/** Session c1ee28 — Google Connect debug instrumentation (remove after verified fix). */
const DEBUG_ENDPOINT = 'http://127.0.0.1:7734/ingest/0871e8fa-3f7a-47da-bc93-ba8ad5f03982';
const DEBUG_SESSION = 'c1ee28';

export function agentDebugLog(
  hypothesisId: string,
  location: string,
  message: string,
  data: Record<string, unknown> = {},
  runId = 'browser',
): void {
  fetch(DEBUG_ENDPOINT, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': DEBUG_SESSION },
    body: JSON.stringify({
      sessionId: DEBUG_SESSION,
      runId,
      hypothesisId,
      location,
      message,
      data,
      timestamp: Date.now(),
    }),
  }).catch(() => {});
}
