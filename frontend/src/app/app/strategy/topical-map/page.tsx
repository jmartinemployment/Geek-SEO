import { IntegrationRequired } from '@/components/app/integration-required';

export default function TopicalMapPage() {
  return (
    <IntegrationRequired
      title="Topical map"
      description="Visual content strategy map from GSC queries and keyword clusters. Use Content Planner for keyword clustering today."
      integrationName="Google Search Console + topical map generator"
    />
  );
}
