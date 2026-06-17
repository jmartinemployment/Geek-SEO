import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async redirects() {
    return [
      {
        source: '/app/projects/:projectId',
        destination: '/app/content?projectId=:projectId',
        permanent: false,
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
