export type FeatureModule = {
  id: string;
  title: string;
  description: string;
  href: string;
  iconBg: string;
  iconColor: string;
};

export const FEATURE_MODULES: FeatureModule[] = [
  {
    id: 'content',
    title: 'Content Optimizer',
    description: 'Score and improve content against real competitors',
    href: '/content-writing',
    iconBg: '#E0F2FE',
    iconColor: '#0369A1',
  },
  {
    id: 'topical-map',
    title: 'Topical Map',
    description: 'Discover topic gaps and plan your content strategy',
    href: '/app/strategy/topical-map',
    iconBg: '#F0FDF4',
    iconColor: '#16A34A',
  },
  {
    id: 'site-analyzer',
    title: 'Site Analyzer',
    description: 'Crawl your site and build keyword research packs',
    href: '/site-analyzer',
    iconBg: '#EEF2FF',
    iconColor: '#4F46E5',
  },
  {
    id: 'keywords',
    title: 'Keyword Research',
    description: 'Find keywords your competitors rank for',
    href: '/app/keywords',
    iconBg: '#FDF4FF',
    iconColor: '#9333EA',
  },
  {
    id: 'rankings',
    title: 'Rank Tracker',
    description: 'Track your positions in Google automatically',
    href: '/app/rankings',
    iconBg: '#FFF7ED',
    iconColor: '#EA580C',
  },
  {
    id: 'audit',
    title: 'Site Audit',
    description: 'Find and fix technical SEO issues',
    href: '/app/audit',
    iconBg: '#FFF1F2',
    iconColor: '#E11D48',
  },
  {
    id: 'analytics',
    title: 'Analytics',
    description: 'GSC + GA4 traffic and performance data',
    href: '/app/analytics',
    iconBg: '#F0F9FF',
    iconColor: '#0284C7',
  },
];

export const SITE_METRIC_COLUMNS = [
  'SEO',
  'Topical Coverage',
  'Site Health',
  'Organic Keywords',
  'Backlinks',
] as const;
