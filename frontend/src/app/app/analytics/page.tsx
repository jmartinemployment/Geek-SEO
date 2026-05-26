import { IntegrationRequired } from '@/components/app/integration-required';

export default function AnalyticsPage() {
  return (
    <IntegrationRequired
      title="Analytics"
      description="Traffic and conversion reporting from Google Analytics 4 after OAuth is configured."
      integrationName="Google Analytics 4"
    />
  );
}
