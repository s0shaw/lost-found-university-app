import Link from "next/link";
import type { ItemReportSummary } from "./types";

export function ReportList({ items }: { items: ItemReportSummary[] }) {
  if (items.length === 0) {
    return <p className="text-ink-muted">No reports match this search.</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {items.map((item) => (
        <li key={item.id} className="card px-4 py-3">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <Link href={`/reports/${item.id}`} className="text-lg text-accent underline">
              {item.title}
            </Link>
            <span className="badge">
              {item.type} · {item.status}
            </span>
          </div>
          <p className="text-sm text-ink-muted">
            {item.categoryName} · {item.locationName} · {formatDate(item.occurredOn)}
          </p>
        </li>
      ))}
    </ul>
  );
}

/** "2026-09-18" to "18 September 2026". Parsed as UTC on purpose: `new Date("2026-01-01")`
    is midnight UTC, which renders as the previous day west of Greenwich. */
export function formatDate(isoDate: string) {
  const [year, month, day] = isoDate.split("-").map(Number);
  const monthName = new Date(Date.UTC(year, month - 1, day)).toLocaleString("en-GB", {
    month: "long",
    timeZone: "UTC",
  });
  return `${day} ${monthName} ${year}`;
}
