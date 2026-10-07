import type { Metadata } from "next";
import Link from "next/link";
import { HealthStatus } from "@/features/health/health-status";
import "./globals.css";

export const metadata: Metadata = {
  title: "University Lost & Found",
  description: "Report a lost or found item on university and track it to its owner.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body className="antialiased">
        <header className="bg-brand text-on-brand">
          <div className="mx-auto flex max-w-3xl flex-wrap items-center justify-between gap-3 px-4 py-4">
            <Link href="/" className="text-lg font-extrabold tracking-tight">
              University Lost &amp; Found
            </Link>
            <nav aria-label="Main">
              <ul className="flex gap-1">
                <li>
                  <Link
                    href="/reports"
                    className="rounded-lg px-3 py-1.5 font-medium text-on-brand hover:bg-white/15"
                  >
                    Reports
                  </Link>
                </li>
                <li>
                  <Link
                    href="/track"
                    className="rounded-lg px-3 py-1.5 font-medium text-on-brand hover:bg-white/15"
                  >
                    Track
                  </Link>
                </li>
                <li>
                  <Link
                    href="/staff"
                    className="rounded-lg px-3 py-1.5 font-medium text-on-brand hover:bg-white/15"
                  >
                    Staff
                  </Link>
                </li>
              </ul>
            </nav>
          </div>
        </header>
        <main className="mx-auto max-w-3xl px-4 py-8">{children}</main>
        <footer className="mt-12 border-t border-line">
          {/* Awaited, not wrapped in Suspense: streaming a fallback from the layout would flush the
              HTTP status before a page could call notFound(). */}
          <div className="mx-auto flex max-w-3xl flex-wrap justify-between gap-2 px-4 py-6 text-sm text-ink-muted">
            <p>University Lost &amp; Found — demo project. Fictional data only.</p>
            <HealthStatus />
          </div>
        </footer>
      </body>
    </html>
  );
}
