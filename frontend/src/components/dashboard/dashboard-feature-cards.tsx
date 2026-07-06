import Link from 'next/link';
import {
  BarChart3,
  Compass,
  FileSearch,
  Map,
  PenLine,
  Search,
  ShieldCheck,
  Sparkles,
} from 'lucide-react';
import { FEATURE_MODULES } from '@/components/dashboard/dashboard.constants';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';

const moduleIcons = {
  'topical-map': Map,
  keywords: Search,
  rankings: BarChart3,
  audit: ShieldCheck,
  analytics: FileSearch,
  niche: Compass,
} as const;

export function DashboardFeatureCards() {
  return (
    <section aria-label="Feature modules">
      <div className="-mx-1 flex gap-3 overflow-x-auto px-1 pb-1">
        {FEATURE_MODULES.map((module) => {
          const Icon = moduleIcons[module.id as keyof typeof moduleIcons] ?? Sparkles;
          return (
            <Link key={module.id} href={module.href} prefetch={false} className="shrink-0">
              <Card className="h-full w-40 hover:border-[var(--color-border-strong)]">
                <CardHeader className="gap-3 pb-0">
                  <div
                    className="flex size-10 items-center justify-center rounded-xl"
                    style={{ backgroundColor: module.iconBg, color: module.iconColor }}
                  >
                    <Icon className="size-5" />
                  </div>
                  <CardTitle className="text-sm">{module.title}</CardTitle>
                </CardHeader>
                <CardContent className="pt-2">
                  <CardDescription className="line-clamp-2 text-xs leading-5">
                    {module.description}
                  </CardDescription>
                </CardContent>
              </Card>
            </Link>
          );
        })}
      </div>
    </section>
  );
}
