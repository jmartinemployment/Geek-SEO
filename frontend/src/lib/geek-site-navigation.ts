export type GeekSiteNavLink = {
  label: string;
  href: string;
  external?: boolean;
  children?: GeekSiteNavLink[];
};

const MAIN_SITE = 'https://www.geekatyourspot.com';

export const geekSiteNavLinks: GeekSiteNavLink[] = [
  { label: 'Home', href: `${MAIN_SITE}/`, external: true },
  { label: 'Geek SEO', href: '/app/dashboard' },
  {
    label: 'Use Cases',
    href: `${MAIN_SITE}/use-cases`,
    external: true,
    children: [
      {
        label: 'Accounting',
        href: `${MAIN_SITE}/use-cases/accounting`,
        external: true,
      },
      {
        label: 'Customer Service',
        href: `${MAIN_SITE}/use-cases/customer-service`,
        external: true,
      },
      {
        label: 'Human Resources',
        href: `${MAIN_SITE}/use-cases/human-resources`,
        external: true,
      },
      {
        label: 'Marketing',
        href: `${MAIN_SITE}/use-cases/marketing`,
        external: true,
      },
    ],
  },
];

export const geekSiteFooterLinks = {
  home: MAIN_SITE,
  privacy: `${MAIN_SITE}/privacy-policy`,
  terms: `${MAIN_SITE}/terms-and-conditions`,
  facebook: 'https://www.facebook.com/GeekAtYourSpot/',
  linkedin: 'https://www.linkedin.com/company/geekatyourspot',
  email: 'mailto:info@geekatyourspot.com',
  phone: 'tel:+15615263512',
  map: 'https://share.google/N5czXCIcHvptENeqr',
};
