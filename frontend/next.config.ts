import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  async redirects() {
    return [
      {
        source: '/app/projects/:projectId',
        destination: '/app/content?projectId=:projectId',
        permanent: false,
      },
    ];
  },
};

export default nextConfig;
