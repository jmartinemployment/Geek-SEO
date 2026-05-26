import { IntegrationRequired } from '@/components/app/integration-required';

export default function RankingsPage() {
  return (
    <IntegrationRequired
      title="GSC rankings"
      description="Keyword position table with 90-day trends, impressions, CTR, and open-in-editor actions — powered by Google Search Console once connected."
      integrationName="Google Search Console"
    />
  );
}
