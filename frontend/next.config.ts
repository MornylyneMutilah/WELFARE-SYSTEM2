import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Container builds use the minimal server; local builds keep npm start working.
  output: process.env.DOCKER_BUILD === "true" ? "standalone" : undefined,
};

export default nextConfig;
