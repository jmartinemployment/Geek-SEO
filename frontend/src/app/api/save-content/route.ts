import { NextRequest, NextResponse } from 'next/server';
import path from 'path';
import fs from 'fs/promises';

type SaveContentBody = {
  keyword: string;
  pillarHtml: string;
  blogPosts: Array<{ slug: string; html: string; title: string }>;
  socialText?: string | null;
};

const REPO_DATA = process.env.CONTENT_REPO_DATA_DIR
  ?? '/Users/jeffmartin/Documents/geekatyourspot-r/data';

export async function POST(req: NextRequest) {
  const body = (await req.json()) as SaveContentBody;
  const { keyword, pillarHtml, blogPosts, socialText } = body;

  if (!keyword || !pillarHtml) {
    return NextResponse.json({ error: 'keyword and pillarHtml required' }, { status: 400 });
  }

  const slug = keyword.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');

  try {
    const saved: string[] = [];

    // Pillar → data/use-cases/{slug}.html
    const useCasesDir = path.join(REPO_DATA, 'use-cases');
    await fs.mkdir(useCasesDir, { recursive: true });
    await fs.writeFile(path.join(useCasesDir, `${slug}.html`), pillarHtml, 'utf-8');
    saved.push(`use-cases/${slug}.html`);

    // Blog posts → data/blog/posts/{slug}.html
    if (blogPosts?.length) {
      const blogDir = path.join(REPO_DATA, 'blog', 'posts');
      await fs.mkdir(blogDir, { recursive: true });
      for (const post of blogPosts) {
        const postSlug = post.slug || slug + '-blog';
        await fs.writeFile(path.join(blogDir, `${postSlug}.html`), post.html, 'utf-8');
        saved.push(`blog/posts/${postSlug}.html`);
      }
    }

    // Social → data/social/{slug}.txt
    if (socialText) {
      const socialDir = path.join(REPO_DATA, 'social');
      await fs.mkdir(socialDir, { recursive: true });
      await fs.writeFile(path.join(socialDir, `${slug}.txt`), socialText, 'utf-8');
      saved.push(`social/${slug}.txt`);
    }

    return NextResponse.json({ saved: true, files: saved, root: REPO_DATA });
  } catch {
    return NextResponse.json({ saved: false, fallback: true });
  }
}
