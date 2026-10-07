import Link from "next/link";
import { listStaffClaims, listStaffReports } from "@/features/staff/api-server";
import { QueueList, type BulkAction } from "@/features/staff/queue-list";
import type { StaffQueue } from "@/features/staff/types";
import { Pagination } from "@/features/reports/pagination";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

const PAGE_SIZE = 50;

const queues: { value: StaffQueue; label: string; blurb: string; bulk: BulkAction | null }[] = [
  {
    value: "PendingHandover",
    label: "Awaiting handover",
    blurb: "Found reports waiting for the item to arrive at its collection point.",
    bulk: { action: "confirm-handover", label: "Confirm handover" },
  },
  {
    value: "Stale",
    label: "Stale",
    blurb: "Open reports older than 30 days. Close them, or leave them another week.",
    bulk: { action: "close", label: "Close selected", reason: true },
  },
  {
    value: "PossibleDuplicate",
    label: "Possible duplicates",
    blurb: "Reports that look like another report in the same category and place.",
    bulk: null,
  },
];

function isQueue(value: string | undefined): value is StaffQueue {
  return queues.some((queue) => queue.value === value);
}

type SearchParams = Promise<Record<string, string | undefined>>;

export default async function StaffQueuesPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  // The tab is the query string, so the server component refetches and the URL is shareable.
  const queue = isQueue(params.queue) ? params.queue : "PendingHandover";
  const active = queues.find((item) => item.value === queue)!;

  let result;
  let counts: number[] | null = null;
  let pendingClaims: number | null = null;
  let error: string | null = null;
  try {
    // Counts come from the same list endpoints (page size 1, only totalCount is read).
    const [list, queueCounts, claims] = await Promise.all([
      listStaffReports(queue, Number(params.page) || 1, PAGE_SIZE),
      Promise.all(queues.map((item) => listStaffReports(item.value, 1, 1))),
      listStaffClaims("Pending", 1, 1),
    ]);
    result = list;
    counts = queueCounts.map((item) => item.totalCount);
    pendingClaims = claims.totalCount;
  } catch (caught) {
    const fieldMessages =
      caught instanceof ApiError ? Object.values(caught.problem.errors ?? {}).flat() : [];
    error =
      fieldMessages.length > 0
        ? fieldMessages.join(" ")
        : "The reports service is unavailable. Try again in a moment.";
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-extrabold tracking-tight">Queues</h1>

      <nav aria-label="Queues">
        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {queues.map((item, index) => (
            <li key={item.value}>
              <Link
                href={`/staff?queue=${item.value}`}
                aria-current={item.value === queue ? "page" : undefined}
                className={`card block px-4 py-3 ${item.value === queue ? "border-accent! bg-accent-soft" : ""}`}
              >
                <span className="block text-3xl font-extrabold">{counts?.[index] ?? "–"}</span>
                <span className="block font-medium">{item.label}</span>
              </Link>
            </li>
          ))}
          <li>
            <Link href="/staff/claims" className="card block px-4 py-3">
              <span className="block text-3xl font-extrabold">{pendingClaims ?? "–"}</span>
              <span className="block font-medium">Pending claims</span>
            </Link>
          </li>
        </ul>
      </nav>

      <p className="text-ink-muted">{active.blurb}</p>

      {error ? (
        <p role="alert" className="text-error">
          {error}
        </p>
      ) : result!.items.length === 0 ? (
        <p className="text-ink-muted">This queue is empty.</p>
      ) : (
        <>
          <QueueList key={queue} items={result!.items} bulk={active.bulk} />
          <Pagination
            page={result!.page}
            pageSize={result!.pageSize}
            totalCount={result!.totalCount}
            params={{ queue }}
            basePath="/staff"
          />
        </>
      )}
    </div>
  );
}
