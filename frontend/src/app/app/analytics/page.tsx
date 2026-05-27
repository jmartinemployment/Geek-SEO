import { GoogleProjectPanel } from '@/components/google/google-project-panel';

export default function AnalyticsPage() {
  return (
    <GoogleProjectPanel
      title="Analytics"
      description="Landing page sessions and conversions from Google Analytics 4 for the selected project."
      mode="analytics"
    />
  );
}
