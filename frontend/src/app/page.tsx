import Link from 'next/link';

export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl flex-col justify-center gap-6 p-8">
      <h1 className="text-4xl font-semibold tracking-tight">Geek SEO</h1>
      <p className="text-lg text-zinc-600">
        AI content optimization for small businesses — transparent scoring, local SERP, WordPress publish.
      </p>
      <div className="flex gap-3">
        <Link
          href="/auth/login"
          className="inline-flex w-fit rounded-lg bg-zinc-900 px-5 py-3 text-white hover:bg-zinc-800"
        >
          Sign in
        </Link>
        <Link
          href="/app/projects"
          className="inline-flex w-fit rounded-lg border px-5 py-3 hover:bg-zinc-50"
        >
          Dashboard
        </Link>
      </div>
    </main>
  );
}
