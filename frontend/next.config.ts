import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "standalone", // self-contained build for the Docker image

  async headers() {
    const dev = process.env.NODE_ENV !== "production";
    // Next inlines small bootstrap scripts, so script-src needs 'unsafe-inline' without a nonce setup.
    const csp = [
      "default-src 'self'",
      `script-src 'self' 'unsafe-inline'${dev ? " 'unsafe-eval'" : ""}`,
      "style-src 'self' 'unsafe-inline'",
      "img-src 'self' data:",
      `connect-src 'self'${dev ? " ws:" : ""}`,
      "object-src 'none'",
      "base-uri 'self'",
      "form-action 'self'",
      "frame-ancestors 'none'",
    ].join("; ");

    return [
      {
        source: "/:path*",
        headers: [
          { key: "Content-Security-Policy", value: csp },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
          { key: "Referrer-Policy", value: "no-referrer" },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
          { key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" },
        ],
      },
    ];
  },

  // Keeps browser calls same-origin so the staff session cookie is first-party (Safari blocks
  // third-party cookies outright). Resolved at build time and baked into routes-manifest.json,
  // so API_URL has to be present when `next build` runs — not only at runtime.
  async rewrites() {
    const api = process.env.API_URL ?? "http://localhost:5000";
    return [{ source: "/api/:path*", destination: `${api}/api/:path*` }];
  },
};

export default nextConfig;
