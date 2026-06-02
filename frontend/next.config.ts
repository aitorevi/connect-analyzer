import type { NextConfig } from "next";

// Security headers for the public demo. The frontend fetches the backend server-side, so
// the browser only talks to its own origin → connect-src 'self'. 'unsafe-inline' is kept
// for script/style because Next (App Router hydration) and Recharts emit inline ones and
// this demo doesn't wire up CSP nonces.
//
// 'unsafe-eval' is added ONLY in development: Next's dev runtime uses eval() (Fast Refresh,
// dev source maps) and without it React fails to render client components in dev (Recharts
// charts come up blank). Production builds never use eval(), so the prod CSP stays strict.
const isDev = process.env.NODE_ENV !== "production";

const csp = [
  "default-src 'self'",
  `script-src 'self' 'unsafe-inline'${isDev ? " 'unsafe-eval'" : ""}`,
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data:",
  "font-src 'self'",
  "connect-src 'self'",
  "base-uri 'self'",
  "form-action 'self'",
  "frame-ancestors 'none'",
].join("; ");

const securityHeaders = [
  { key: "Content-Security-Policy", value: csp },
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "DENY" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  {
    key: "Permissions-Policy",
    value: "camera=(), microphone=(), geolocation=()",
  },
];

const nextConfig: NextConfig = {
  // Dev-only: allow the dev server's client/HMR resources when the app is opened via these
  // hosts. Without this, opening http://127.0.0.1:3000 (instead of localhost) gets its
  // /_next dev resources blocked cross-origin and the Recharts charts fail to load. No effect
  // on production builds.
  allowedDevOrigins: ["127.0.0.1", "localhost"],
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default nextConfig;
