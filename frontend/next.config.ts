import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async redirects() {
    return [
      {
        source: '/app/strategy/topical-map',
        destination: '/app/dashboard',
        permanent: false,
      },
      {
        source: '/app/audit',
        destination: '/app/dashboard',
        permanent: false,
      },
    ];
  },
};

export default nextConfig;
