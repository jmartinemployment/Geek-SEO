import type { ComponentType } from 'react';
import {
  FileText,
  Home,
  LayoutGrid,
  LineChart,
  Search,
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

export const sidebarPrimaryNav: SidebarPrimaryNav[] = [
  {
    id: 'home',
    label: 'Home',
    icon: Home,
    sections: [
      {
        items: [{ href: '/app/dashboard', label: 'Home' }],
      },
    ],
  },
  {
    id: 'create',
    label: 'Create',
    icon: Sparkles,
    sections: [
      {
        items: [
          { href: '/content-writing', label: 'Research' },
          { href: '/content-writing', label: 'Content' },
          { href: PLACEHOLDER_HREF, label: 'Opportunities' },
          { href: PLACEHOLDER_HREF, label: 'Clusters' },
        ],
      },
    ],
  },
  {
    id: 'site',
    label: 'Site',
    icon: TrendingUp,
    sections: [
      {
        items: [
          { href: PLACEHOLDER_HREF, label: 'Audits' },
          { href: PLACEHOLDER_HREF, label: 'Health' },
          { href: PLACEHOLDER_HREF, label: 'Issues' },
          { href: PLACEHOLDER_HREF, label: 'Content Guard' },
          { href: PLACEHOLDER_HREF, label: 'Answers' },
        ],
      },
    ],
  },
  {
    id: 'analyze',
    label: 'Analyze',
    icon: LineChart,
    sections: [
      {
        items: [
          { href: PLACEHOLDER_HREF, label: 'SEO Analytics' },
          { href: PLACEHOLDER_HREF, label: 'AI Visibility' },
        ],
      },
    ],
  },
  {
    id: 'search',
    label: 'Search',
    icon: Search,
    sections: [],
  },
  {
    id: 'content',
    label: 'Content',
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
  return 'create';
}

export function defaultActiveSecondaryLabel(primaryId: string): string | null {
  const primary = sidebarPrimaryNav.find((item) => item.id === primaryId);
  return primary?.sections[0]?.items[0]?.label ?? null;
}

export function isPlaceholderHref(href: string): boolean {
  return href === PLACEHOLDER_HREF;
}
