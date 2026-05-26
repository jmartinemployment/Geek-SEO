import { IntegrationRequired } from '@/components/app/integration-required';

export default function ContentGuardPage() {
  return (
    <IntegrationRequired
      title="Content Guard"
      description="Monitor published URLs for drift, broken links, and score regression. Requires GSC URL inventory and scheduled checks."
      integrationName="Content Guard monitors"
    />
  );
}
