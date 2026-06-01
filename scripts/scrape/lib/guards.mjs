/** Hostnames used only in docs/smoke tests — not competitor research. */
const SMOKE_HOSTS = new Set(['example.com', 'example.org', 'example.net', 'iana.org']);

/**
 * @param {string} url
 */
export function isSmokeTestUrl(url) {
  try {
    const host = new URL(url).hostname.replace(/^www\./, '');
    return SMOKE_HOSTS.has(host);
  } catch {
    return false;
  }
}

/**
 * @param {string} url
 * @param {{ smoke?: boolean }} opts
 */
export function assertResearchUrl(url, opts = {}) {
  if (opts.smoke || !isSmokeTestUrl(url)) return;

  console.error(`Refusing to scrape documentation placeholder: ${url}

example.com is not a competitor — it only proves Playwright works.
To run the smoke test anyway:
  npm run scrape -- page --url "${url}" --out ./docs/research/competitors/_test/example --smoke

For real competitor research:
  npm run scrape:seranking
  npm run scrape -- page --url "https://seranking.com/position-tracking.html" --out ./docs/research/competitors/seranking/position-tracking --network
`);
  process.exit(1);
}
