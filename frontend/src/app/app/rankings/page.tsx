import { GoogleProjectPanel } from '@/components/google/google-project-panel';

export default function RankingsPage() {
  return (
    <GoogleProjectPanel
      title="GSC rankings"
      description="Keyword positions, impressions, and clicks from Google Search Console for the selected project."
      mode="rankings"
    />
  );
}
