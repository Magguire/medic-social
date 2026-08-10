/** @type {import(''next'').NextConfig} */
const apiHost = process.env.API_PROXY_TARGET || 'http://localhost:5241';

const nextConfig = {
  output: 'standalone',
  reactStrictMode: true,
  typescript: {
    tsconfigPath: './tsconfig.json',
  },
  async rewrites() {
    return [
      {
        source: '/api/:path*',
        destination: apiHost + '/api/:path*',
      },
    ];
  },
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
      },
    ],
  },
  async headers() {
    return [
      {
        source: '/:path*',
        headers: [
          { key: 'X-Content-Type-Options', value: 'nosniff' },
          { key: 'X-Frame-Options', value: 'DENY' },
          { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
        ],
      },
    ];
  },
};

module.exports = nextConfig;
