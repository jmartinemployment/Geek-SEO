/**
 * In-browser extraction for research / code-planning artifacts.
 * @param {string} selector Optional root selector (default: main content heuristics)
 */
export function extractPageScript(selector) {
  return (sel) => {
    const pickRoot = () => {
      if (sel) {
        const el = document.querySelector(sel);
        if (el) return el;
      }
      return (
        document.querySelector('main,[role="main"],article') ?? document.body
      );
    };

    const root = pickRoot();

    const meta = {};
    for (const m of document.querySelectorAll('meta[name],meta[property]')) {
      const key = m.getAttribute('name') ?? m.getAttribute('property');
      const content = m.getAttribute('content');
      if (key && content) meta[key] = content.slice(0, 500);
    }

    const headings = [];
    for (const h of document.querySelectorAll('h1,h2,h3,h4,h5,h6')) {
      const level = Number.parseInt(h.tagName[1], 10);
      const text = h.textContent?.trim();
      if (text) headings.push({ level, text: text.slice(0, 300) });
    }

    const links = [];
    for (const a of root.querySelectorAll('a[href]')) {
      const href = a.getAttribute('href');
      const text = a.textContent?.trim().slice(0, 120);
      if (href) links.push({ href, text: text || undefined });
    }

    const textContent = (root.innerText ?? '')
      .replace(/\r\n/g, '\n')
      .replace(/[ \t]+/g, ' ')
      .replace(/\n{3,}/g, '\n\n')
      .trim()
      .slice(0, 50_000);

    const outline = [];
    function walk(el, depth) {
      if (depth > 4) return;
      const tag = el.tagName?.toLowerCase();
      if (!tag || ['script', 'style', 'svg', 'path'].includes(tag)) return;
      const entry = {
        tag,
        id: el.id || undefined,
        className: typeof el.className === 'string' ? el.className.slice(0, 80) : undefined,
        role: el.getAttribute('role') || undefined,
      };
      const kids = Array.from(el.children).slice(0, 15);
      if (kids.length) entry.children = kids.map((c) => walk(c, depth + 1)).filter(Boolean);
      return entry;
    }
    outline.push(walk(root, 0));

    return {
      url: location.href,
      title: document.title,
      lang: document.documentElement.lang || undefined,
      meta,
      headings,
      links,
      textLength: textContent.length,
      textPreview: textContent.slice(0, 2000),
      framework: {
        nextData: !!document.getElementById('__NEXT_DATA__'),
        nuxt: !!window.__NUXT__,
        reactRoot: !!document.querySelector('#__next,#root,[data-reactroot]'),
      },
      domOutline: outline[0],
      scrapedAt: new Date().toISOString(),
    };
  };
}

/**
 * @param {import('playwright').Page} page
 * @param {{ selector?: string; captureNetwork?: boolean }} opts
 */
export async function extractPage(page, opts = {}) {
  const network = [];
  if (opts.captureNetwork) {
    page.on('request', (req) => {
      const type = req.resourceType();
      if (type === 'xhr' || type === 'fetch') {
        network.push({ method: req.method(), url: req.url() });
      }
    });
  }

  const data = await page.evaluate(extractPageScript(), opts.selector ?? null);

  return { data, network: network.slice(0, 300) };
}

/**
 * @param {string} href
 * @param {string} pageUrl
 */
function resolveLinkHref(href, pageUrl) {
  try {
    return new URL(href, pageUrl).href;
  } catch {
    return href;
  }
}

/**
 * @param {string} href
 * @param {string} pageUrl
 */
function isSameOriginLink(href, pageUrl) {
  try {
    return new URL(href, pageUrl).origin === new URL(pageUrl).origin;
  } catch {
    return false;
  }
}

/**
 * @param {{ title: string; url: string; headings: { level: number; text: string }[]; textPreview: string; textLength?: number; links: { href: string; text?: string }[]; meta?: Record<string, string> }} data
 * @param {{ fullText?: string }} [opts]
 */
export function toMarkdown(data, opts = {}) {
  const body = (opts.fullText ?? data.textPreview ?? '').trim();
  const bodyTruncated = body.length > 48_000;
  const bodyForMd = bodyTruncated ? `${body.slice(0, 48_000)}\n\n_[truncated — see full-text.txt]_` : body;

  const description =
    data.meta?.description ?? data.meta?.['og:description'] ?? data.meta?.['twitter:description'];

  const lines = [
    `# ${data.title}`,
    '',
    `**URL:** ${data.url}`,
    '',
    '> **Agent note:** This file is the primary research digest. Also open `full-text.txt` (complete visible copy), `page.json` (structured data + DOM outline), `raw.html` (rendered HTML), and `screenshots/viewport.png`.',
    '',
  ];

  if (description) {
    lines.push('## Meta', '', description, '');
  }

  const interestingMeta = ['keywords', 'og:title', 'og:image', 'robots'];
  const metaExtras = interestingMeta
    .map((k) => (data.meta?.[k] ? `- **${k}:** ${data.meta[k]}` : null))
    .filter(Boolean);
  if (metaExtras.length) {
    lines.push(...metaExtras, '');
  }

  lines.push('## Headings', '');
  if (data.headings.length === 0) {
    lines.push('_No headings found — page may be empty or blocked._', '');
  } else {
    for (const h of data.headings) {
      lines.push(`${'#'.repeat(h.level)} ${h.text}`);
    }
    lines.push('');
  }

  lines.push('## Page copy', '');
  if (bodyForMd) {
    const paragraphs = bodyForMd.split(/\n\n+/).filter((p) => p.trim());
    if (paragraphs.length > 1) {
      for (const p of paragraphs) {
        lines.push(p.trim(), '');
      }
    } else {
      lines.push(bodyForMd, '');
    }
  } else {
    lines.push('_No visible text extracted._', '');
  }

  const internal = [];
  const external = [];
  for (const link of data.links) {
    const abs = resolveLinkHref(link.href, data.url);
    const entry = { ...link, href: abs };
    if (isSameOriginLink(link.href, data.url)) internal.push(entry);
    else external.push(entry);
  }

  const pushLinks = (title, items, max = 80) => {
    lines.push(`## ${title}`, '');
    if (items.length === 0) {
      lines.push('_None._', '');
      return;
    }
    for (const link of items.slice(0, max)) {
      const label = (link.text ?? link.href).replace(/\s+/g, ' ').trim();
      lines.push(`- [${label}](${link.href})`);
    }
    if (items.length > max) {
      lines.push(`- _…and ${items.length - max} more (see links.json)_`);
    }
    lines.push('');
  };

  pushLinks(`Same-site links (${internal.length})`, internal);
  pushLinks(`External links (${external.length})`, external, 30);

  lines.push(
    '## Artifacts',
    '',
    '| File | Purpose |',
    '|------|---------|',
    '| `SCRAPE-REPORT.md` | Scrape summary and file index |',
    '| `full-text.txt` | Full visible text from the browser |',
    '| `page.json` | Title, meta, headings, links, DOM outline |',
    '| `raw.html` | Rendered HTML snapshot |',
    '| `links.json` | All links (machine-readable) |',
    '| `screenshots/viewport.png` | Visual proof of what loaded |',
    '',
    `**Stats:** ${data.headings?.length ?? 0} headings · ${data.links?.length ?? 0} links · ${data.textLength ?? body.length} characters of text`,
    '',
  );

  return lines.join('\n');
}
