import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Enable standalone output for optimized production deployment
  output: 'standalone',
  turbopack: {
    root: __dirname,
  },
  // Configure webpack to handle pdf.js worker
  webpack: (config) => {
    // Ensure pdf.js worker is properly bundled
    config.resolve.alias.canvas = false;
    return config;
  },
  // Performance optimizations
  poweredByHeader: false, // Remove X-Powered-By header for security
  compress: true, // Enable gzip compression
  reactStrictMode: true, // Enable React strict mode for better debugging
  // Enable optimizations for production
  productionBrowserSourceMaps: false, // Disable source maps in production for security
  // Configure image optimization
  images: {
    formats: ['image/avif', 'image/webp'],
    deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048, 3840],
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
  },
  // Enable experimental features for better performance
  experimental: {
    optimizePackageImports: ['antd', '@ant-design/icons', 'dayjs'],
  },
  // Configure headers for security and caching
  async headers() {
    return [
      {
        source: '/(.*)',
        headers: [
          {
            key: 'X-DNS-Prefetch-Control',
            value: 'on',
          },
        ],
      },
      {
        // Cache static assets aggressively
        source: '/(.*)\\.(ico|png|jpg|jpeg|gif|svg|woff|woff2)',
        headers: [
          {
            key: 'Cache-Control',
            value: 'public, max-age=31536000, immutable',
          },
        ],
      },
    ];
  },
};

export default nextConfig;
