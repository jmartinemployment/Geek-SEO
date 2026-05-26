import { IntegrationRequired } from '@/components/app/integration-required';

export default function AuditPage() {
  return (
    <IntegrationRequired
      title="Site audit"
      description="Crawl-based technical SEO audit with issue prioritization. Requires Playwright crawler and site crawl jobs."
      integrationName="Site crawl pipeline"
    />
  );
}
