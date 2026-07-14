import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: '/content-writer',
        destination: 'https://content-writer-jeff-martins-projects-66716453.vercel.app',
      },
      {
        source: '/content-writer/:path*',
        destination: 'https://content-writer-jeff-martins-projects-66716453.vercel.app/:path*',
      },
    ];
  },
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
