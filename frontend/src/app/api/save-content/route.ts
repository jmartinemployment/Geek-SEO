import { NextRequest, NextResponse } from 'next/server';
import path from 'path';
import fs from 'fs/promises';

type SaveContentBody = {
  keyword: string;
  pillarHtml: string;
  blogPosts: Array<{ slug: string; html: string; title: string }>;
  socialText?: string;
};

const OUTPUT_DIR = process.env.CONTENT_OUTPUT_DIR
  ?? '/Users/jeffmartin/Documents/Geek-SEO/docs/content-writing';

export async function POST(req: NextRequest) {
  const body = (await req.json()) as SaveContentBody;
  const { keyword, pillarHtml, blogPosts, socialText } = body;

  if (!keyword || !pillarHtml) {
    return NextResponse.json({ error: 'keyword and pillarHtml required' }, { status: 400 });
  }

  const slug = keyword.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  const dir = path.join(OUTPUT_DIR, slug);

  try {
    await fs.mkdir(dir, { recursive: true });
    await fs.writeFile(path.join(dir, 'pillar.html'), pillarHtml, 'utf-8');

    for (const post of blogPosts ?? []) {
      const postSlug = post.slug || 'blog-post';
      await fs.writeFile(path.join(dir, `${postSlug}.html`), post.html, 'utf-8');
    }

    if (socialText) {
      await fs.writeFile(path.join(dir, 'social.txt'), socialText, 'utf-8');
    }

    await fs.writeFile(
      path.join(dir, 'metadata.json'),
      JSON.stringify({
        keyword,
        savedAt: new Date().toISOString(),
        blogPosts: (blogPosts ?? []).map((p) => ({ slug: p.slug, title: p.title })),
      }, null, 2),
      'utf-8',
    );

    return NextResponse.json({ saved: true, dir });
  } catch {
    // Filesystem not writable (e.g. Vercel) — return content for browser download
    return NextResponse.json({ saved: false, fallback: true }, { status: 200 });
  }
}
