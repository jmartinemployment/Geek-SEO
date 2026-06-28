import type { ComponentType } from 'react';
import {
  FileText,
  Home,
  LayoutGrid,
  LineChart,
  MapPin,
  Megaphone,
  Share2,
  Sparkles,
  TrendingUp,
} from 'lucide-react';

export type SidebarNavItem = {
  href: string;
  label: string;
};

export type SidebarNavSection = {
  title?: string;
  items: SidebarNavItem[];
};

export type SidebarPrimaryNav = {
  id: string;
  label: string;
  icon: ComponentType<{ className?: string; strokeWidth?: number }>;
  sections: SidebarNavSection[];
};

export const SIDEBAR_PRIMARY_WIDTH = 64;
export const SIDEBAR_SECONDARY_WIDTH = 220;

const PLACEHOLDER_HREF = '#';

/** Semrush SEO sidebar — labels and structure duplicated verbatim; hrefs are placeholders only. */
export const sidebarPrimaryNav: SidebarPrimaryNav[] = [
  {
    id: 'home',
    label: 'Home',
    icon: Home,
    sections: [],
  },
  {
    id: 'seo',
    label: 'SEO',
    icon: TrendingUp,
    sections: [
      {
        items: [{ href: PLACEHOLDER_HREF, label: 'Dashboard' }],
      },
      {
        title: 'Site Performance',
        items: [
          { href: PLACEHOLDER_HREF, label: 'Site Audit' },
          { href: PLACEHOLDER_HREF, label: 'Position Tracking' },
        ],
      },
      {
        title: 'Competitive Analysis',
        items: [
          { href: PLACEHOLDER_HREF, label: 'Domain Overview' },
          { href: PLACEHOLDER_HREF, label: 'Organic Rankings' },
          { href: PLACEHOLDER_HREF, label: 'Top Pages' },
          { href: PLACEHOLDER_HREF, label: 'Compare Domains' },
          { href: PLACEHOLDER_HREF, label: 'Keyword Gap' },
          { href: PLACEHOLDER_HREF, label: 'Backlink Gap' },
        ],
      },
      {
        title: 'Keyword Research',
        items: [
          { href: PLACEHOLDER_HREF, label: 'Keyword Overview' },
          { href: PLACEHOLDER_HREF, label: 'Keyword Magic Tool' },
          { href: PLACEHOLDER_HREF, label: 'Keyword Strategy Builder' },
        ],
      },
      {
        title: 'Content Ideas',
        items: [
          { href: PLACEHOLDER_HREF, label: 'SEO Writing Assistant' },
          { href: PLACEHOLDER_HREF, label: 'Topic Research' },
          { href: PLACEHOLDER_HREF, label: 'SEO Content Template' },
        ],
      },
      {
        title: 'Link Building',
        items: [
          { href: PLACEHOLDER_HREF, label: 'Backlinks' },
          { href: PLACEHOLDER_HREF, label: 'Referring Domains' },
          { href: PLACEHOLDER_HREF, label: 'Backlink Audit' },
        ],
      },
    ],
  },
  {
    id: 'ai',
    label: 'AI',
    icon: Sparkles,
    sections: [],
  },
  {
    id: 'traffic',
    label: 'Traffic & Market',
    icon: LineChart,
    sections: [],
  },
  {
    id: 'local',
    label: 'Local',
    icon: MapPin,
    sections: [],
  },
  {
    id: 'content',
    label: 'Content',
    icon: FileText,
    sections: [],
  },
  {
    id: 'ad',
    label: 'Ad',
    icon: Megaphone,
    sections: [],
  },
  {
    id: 'ai-pr',
    label: 'AI PR',
    icon: Sparkles,
    sections: [],
  },
  {
    id: 'social',
    label: 'Social',
    icon: Share2,
    sections: [],
  },
  {
    id: 'reports',
    label: 'Reports',
    icon: FileText,
    sections: [],
  },
  {
    id: 'app-center',
    label: 'App Center',
    icon: LayoutGrid,
    sections: [],
  },
];

export function defaultActivePrimaryId(): string {
  return 'seo';
}

export function defaultActiveSecondaryLabel(primaryId: string): string | null {
  const primary = sidebarPrimaryNav.find((item) => item.id === primaryId);
  return primary?.sections[0]?.items[0]?.label ?? null;
}

export function isPlaceholderHref(href: string): boolean {
  return href === PLACEHOLDER_HREF;
}
