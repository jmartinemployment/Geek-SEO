/** Debug session logger — posts NDJSON to Cursor ingest (session c1ee28). */
export function agentDebugLog(
  hypothesisId: string,
  location: string,
  message: string,
  data: Record<string, unknown> = {},
): void {
  // #region agent log
  fetch('http://127.0.0.1:7734/ingest/0871e8fa-3f7a-47da-bc93-ba8ad5f03982', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-Debug-Session-Id': 'c1ee28',
    },
    body: JSON.stringify({
      sessionId: 'c1ee28',
      hypothesisId,
      location,
      message,
      data,
      timestamp: Date.now(),
    }),
  }).catch(() => {});
  // #endregion
}
