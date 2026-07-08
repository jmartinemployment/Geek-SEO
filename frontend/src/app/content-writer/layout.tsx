import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Content Writer | Geek SEO',
  description:
    'AI-assisted content generation for IT consulting projects — technical articles, blog posts, social posts, and cold outreach emails with schema.org JSON+LD.',
};

const contentWriterSoftwareApplication = {
  '@context': 'https://schema.org',
  '@type': 'SoftwareApplication',
  name: 'Content Writer',
  applicationCategory: 'BusinessApplication',
  operatingSystem: 'Web',
  url: 'https://seo.geekatyourspot.com/content-writer',
  description:
    'AI-assisted content generation for IT consulting projects. Turn a crawled client site and SERP research into a TechnicalArticle, companion BlogPost, Facebook/LinkedIn social posts, and cold outreach email — with schema.org JSON+LD where applicable.',
  provider: {
    '@type': 'Organization',
    name: 'Geek At Your Spot',
    url: 'https://www.geekatyourspot.com',
  },
};

export default function ContentWriterLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <>
      <script
        type="application/ld+json"
        dangerouslySetInnerHTML={{
          __html: JSON.stringify(contentWriterSoftwareApplication),
        }}
      />
      {children}
    </>
  );
}
