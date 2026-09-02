import type { NextConfig } from "next";

const backendUrl = process.env.BACKEND_URL || "http://localhost:5024";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/proxy/:path*",
        destination: `${backendUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
