import type { ComponentType } from 'react';
import {
  BarChart3,
  FileText,
  FolderKanban,
  Home,
  Map,
  Settings,
  TrendingUp,
} from 'lucide-react';

export type SidebarNavItem = {
  href: string;
  label: string;
};

export type SidebarNavSection = {
  title: string;
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

export const sidebarPrimaryNav: SidebarPrimaryNav[] = [
  {
    id: 'home',
    label: 'Home',
    icon: Home,
    sections: [
      {
        title: 'Overview',
        items: [{ href: '/app/dashboard', label: 'Dashboard' }],
      },
    ],
  },
  {
    id: 'seo',
    label: 'SEO',
    icon: TrendingUp,
    sections: [
      {
        title: 'Site Performance',
        items: [
          { href: '/app/audit', label: 'Site Audit' },
          { href: '/app/rankings', label: 'Rank Tracker' },
        ],
      },
      {
        title: 'Competitive Analysis',
        items: [
          { href: '/site-analyzer', label: 'Site Analyzer' },
          { href: '/url-analyzer', label: 'URL Analyzer' },
          { href: '/app/cannibalization', label: 'Cannibalization' },
        ],
      },
      {
        title: 'Keyword Research',
        items: [
          { href: '/app/keywords', label: 'Keyword Research' },
          { href: '/app/serp', label: 'Deep SERP' },
        ],
      },
    ],
  },
  {
    id: 'content',
    label: 'Content',
    icon: FileText,
    sections: [
      {
        title: 'Content Studio',
        items: [
          { href: '/content-writing', label: 'Content Writing' },
          { href: '/app/content-guard', label: 'Content Guard' },
          { href: '/app/brand-voice', label: 'Brand Voice' },
        ],
      },
      {
        title: 'Production',
        items: [
          { href: '/app/bulk', label: 'Bulk Articles' },
          { href: '/app/calendar', label: 'Content Calendar' },
          { href: '/app/briefs/new', label: 'New Brief' },
        ],
      },
    ],
  },
  {
    id: 'strategy',
    label: 'Strategy',
    icon: Map,
    sections: [
      {
        title: 'Planning',
        items: [
          { href: '/app/strategy/topical-map', label: 'Topical Map' },
          { href: '/app/strategy/niche-analyzer', label: 'Niche Analyzer' },
          { href: '/app/planner', label: 'Planner' },
          { href: '/app/guided', label: 'Guided Workflow' },
        ],
      },
    ],
  },
  {
    id: 'analytics',
    label: 'Analytics',
    icon: BarChart3,
    sections: [
      {
        title: 'Performance',
        items: [
          { href: '/app/analytics', label: 'Analytics' },
          { href: '/app/geo', label: 'GEO' },
        ],
      },
    ],
  },
  {
    id: 'projects',
    label: 'Projects',
    icon: FolderKanban,
    sections: [
      {
        title: 'Workspace',
        items: [{ href: '/app/projects', label: 'All Projects' }],
      },
    ],
  },
];

export const sidebarBottomNav = {
  href: '/app/settings',
  label: 'Settings',
  icon: Settings,
};

function hrefMatchesPath(href: string, pathname: string) {
  if (pathname === href) return true;
  if (href === '/app/dashboard') return false;
  return pathname.startsWith(`${href}/`);
}

export function resolveActivePrimaryId(pathname: string): string {
  if (pathname.startsWith('/app/settings')) {
    return 'home';
  }

  for (const primary of sidebarPrimaryNav) {
    for (const section of primary.sections) {
      for (const item of section.items) {
        if (hrefMatchesPath(item.href, pathname)) {
          return primary.id;
        }
      }
    }
  }

  if (pathname.startsWith('/projects/')) {
    if (pathname.includes('/site-analyzer')) return 'seo';
    if (pathname.includes('/url-analyzer')) return 'seo';
    return 'projects';
  }

  return 'home';
}

export function isSidebarItemActive(href: string, pathname: string) {
  return hrefMatchesPath(href, pathname);
}
