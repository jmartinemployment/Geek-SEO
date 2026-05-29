import { NextResponse } from 'next/server';

const SEO_API_URL = (process.env.NEXT_PUBLIC_SEO_API_URL ?? 'http://localhost:5051').replace(/\/$/u, '');

export async function GET(request: Request) {
  const { searchParams } = new URL(request.url);
  const url = searchParams.get('url')?.trim();
  if (!url) {
    return NextResponse.json({ error: 'Enter a website URL.' }, { status: 400 });
  }

  try {
    const upstream = await fetch(
      `${SEO_API_URL}/api/public/scan?url=${encodeURIComponent(url)}`,
      { next: { revalidate: 0 } },
    );

    const body = await upstream.json();
    return NextResponse.json(body, { status: upstream.status });
  } catch {
    return NextResponse.json(
      {
        error:
          'Scan service is unavailable. Start GeekSeoBackend (port 5051) and try again.',
      },
      { status: 503 },
    );
  }
}
