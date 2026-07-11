import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async redirects() {
    return [
      {
        source: '/app/projects/:projectId',
        destination: '/strategy/topical-map?projectId=:projectId',
        permanent: false,
      },
      {
        source: '/content-writing',
        destination: '/content-writer',
        permanent: true,
      },
      {
        source: '/app/content-writing',
        destination: '/content-writer',
        permanent: true,
      },
      {
        source: '/app/content/:id',
        destination: '/dashboard',
        permanent: true,
      },
      {
        source: '/app/content',
        destination: '/dashboard',
        permanent: true,
      },
      {
        source: '/app/strategy/url-analyzer',
        destination: '/dashboard',
        permanent: true,
      },
      {
        source: '/app/strategy/niche-analyzer',
        destination: '/strategy/topical-map',
        permanent: true,
      },
      {
        source: '/projects/:projectId/url-analyzer',
        destination: '/projects/:projectId',
        permanent: true,
      },
    ];
  },
};

export default nextConfig;
