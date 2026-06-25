/** @type {import('next').NextConfig} */
const nextConfig = {
  reactCompiler: true,

  images: {
    remotePatterns: [
      {
        protocol: "http",
        hostname: 'localhost',
        port: '9000',
        pathname: '/**',
      },
      {
        protocol: "http",
        hostname: 'minio',
        port: '9000',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'ir-8.ozone.ru',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'ir-9.ozone.ru',
        port: '',
        pathname: '/**',
      },
      {
        protocol: 'https',
        hostname: 'ir.ozone.ru',
        port: '',
        pathname: '/**',
      },
    ],
  },

  env: {
    NEXT_PUBLIC_API_BASE_URL: process.env.NEXT_PUBLIC_API_BASE_URL || '',
    NEXT_PUBLIC_KEYCLOAK_URL: process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost/auth',
    NEXT_PUBLIC_KEYCLOAK_REALM: process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'marketplace',
    NEXT_PUBLIC_KEYCLOAK_CLIENT_ID: process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'marketplace-app',
  },
  
  experimental: {
    optimizePackageImports: ['lucide-react', 'keycloak-js'],
  },

};

export default nextConfig;