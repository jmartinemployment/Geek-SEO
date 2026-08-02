import { redirect } from 'next/navigation';

/** Former Site Analyzer entry — product surface retired; keep route for bookmarks. */
export default function SiteAnalyzerRedirectPage() {
  redirect('/strategy/topical-map');
}
