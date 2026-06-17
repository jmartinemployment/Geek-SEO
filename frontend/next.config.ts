import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async redirects() {
    return [
      {
        source: '/app/projects/:projectId',
        destination: '/content-writing?projectId=:projectId',
        permanent: false,
      },
      {
        source: '/app/content-writing',
        destination: '/content-writing',
        permanent: true,
      },
      {
        source: '/app/content/:id',
        destination: '/content-writing?documentId=:id',
        permanent: true,
      },
      {
        source: '/app/content',
        destination: '/content-writing',
        permanent: true,
      },
      {
        source: '/app/strategy/url-analyzer',
        destination: '/url-analyzer',
        permanent: true,
      },
      {
        source: '/app/strategy/niche-analyzer',
        destination: '/url-analyzer',
        permanent: true,
      },
    ];
  },
};

export default nextConfig;
