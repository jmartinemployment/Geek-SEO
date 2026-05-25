import Link from 'next/link';

const tiers = [
  { name: 'Starter', price: '$29', highlights: ['20 documents', '3 full articles', 'Local SERP on every tier'] },
  { name: 'Professional', price: '$59', highlights: ['GSC + GA4', 'Topical map', 'Content audit'] },
  { name: 'Team', price: '$89', highlights: ['Bulk jobs', 'Content Guard', 'Higher GEO limits'] },
  { name: 'Agency', price: '$149', highlights: ['Public API', 'Unlimited caps', 'White-label reports'] },
];

export default function PricingPage() {
  return (
    <main className="mx-auto max-w-5xl px-6 py-16">
      <h1 className="text-3xl font-semibold tracking-tight text-zinc-900">Geek SEO pricing</h1>
      <p className="mt-2 text-zinc-600">
        PayPal subscription checkout ships in Step 51. Tier enforcement runs on GeekSeoBackend today.
      </p>
      <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
        {tiers.map((tier) => (
          <article
            key={tier.name}
            className="rounded-xl border border-zinc-200 bg-white p-6 shadow-sm"
          >
            <h2 className="text-lg font-semibold">{tier.name}</h2>
            <p className="mt-1 text-2xl font-bold text-zinc-900">{tier.price}</p>
            <ul className="mt-4 space-y-2 text-sm text-zinc-600">
              {tier.highlights.map((item) => (
                <li key={item}>• {item}</li>
              ))}
            </ul>
            <button
              type="button"
              disabled
              className="mt-6 w-full cursor-not-allowed rounded-lg bg-zinc-200 px-4 py-2 text-sm font-medium text-zinc-500"
            >
              Subscribe (coming soon)
            </button>
          </article>
        ))}
      </div>
      <Link href="/app/dashboard" className="mt-10 inline-block text-sm text-zinc-600 hover:text-zinc-900">
        ← Back to app
      </Link>
    </main>
  );
}
