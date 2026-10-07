"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";
import { formatDate } from "@/features/reports/report-list";
import { ApiError } from "@/lib/api";
import { staffAction } from "./api";
import type { StaffReportSummary } from "./types";

/** One action applied to every ticked report. `reason` adds the close-reason picker. */
export type BulkAction = { action: "confirm-handover" | "close"; label: string; reason?: boolean };

/** A queue as cards, with a text filter and, where the queue has a natural bulk step
    (confirm handovers, close stale reports), tick boxes. The filter only narrows the page
    already loaded: the API has no search for staff queues. */
export function QueueList({
  items,
  bulk,
}: {
  items: StaffReportSummary[];
  bulk: BulkAction | null;
}) {
  const router = useRouter();
  const [filter, setFilter] = useState("");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [reason, setReason] = useState("Archived");
  const [working, setWorking] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const visible = useMemo(() => {
    const needle = filter.trim().toLowerCase();
    if (!needle) return items;
    return items.filter((item) =>
      [item.title, item.trackingCode, item.categoryName, item.locationName]
        .join(" ")
        .toLowerCase()
        .includes(needle),
    );
  }, [items, filter]);

  const allVisibleSelected = visible.length > 0 && visible.every((item) => selected.has(item.id));

  function toggle(id: string) {
    setSelected((current) => {
      const next = new Set(current);
      if (!next.delete(id)) next.add(id);
      return next;
    });
  }

  function toggleAll() {
    setSelected((current) => {
      const next = new Set(current);
      for (const item of visible) {
        if (allVisibleSelected) next.delete(item.id);
        else next.add(item.id);
      }
      return next;
    });
  }

  async function run() {
    if (!bulk) return;
    setWorking(true);
    setMessage(null);
    const failures: string[] = [];
    for (const id of selected) {
      try {
        await staffAction(
          `/api/staff/reports/${id}/${bulk.action}`,
          bulk.reason ? { reason } : undefined,
        );
      } catch (caught) {
        const title = items.find((item) => item.id === id)?.title ?? id;
        failures.push(`${title}: ${caught instanceof ApiError ? caught.message : "failed"}`);
      }
    }
    const done = selected.size - failures.length;
    setMessage(
      failures.length === 0
        ? `Done: ${done} ${done === 1 ? "report" : "reports"}.`
        : `Done: ${done}. Failed: ${failures.join("; ")}`,
    );
    setSelected(new Set());
    setWorking(false);
    router.refresh();
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex min-w-48 flex-1 flex-col gap-1">
          <label htmlFor="queue-filter" className="text-sm font-bold">
            Filter this page
          </label>
          <input
            id="queue-filter"
            type="search"
            value={filter}
            onChange={(event) => setFilter(event.target.value)}
            placeholder="Title, code, category or place"
            className="field-input"
          />
        </div>
        {bulk && (
          <label className="flex items-center gap-2 pb-2">
            <input
              type="checkbox"
              checked={allVisibleSelected}
              onChange={toggleAll}
              disabled={visible.length === 0}
            />
            Select all shown
          </label>
        )}
      </div>

      {bulk && selected.size > 0 && (
        <div className="card flex flex-wrap items-end gap-3 p-3" role="group" aria-label="Bulk">
          <p className="pb-2 font-bold">{selected.size} selected</p>
          {bulk.reason && (
            <div className="flex flex-col gap-1">
              <label htmlFor="bulk-reason" className="text-sm font-bold">
                Close reason
              </label>
              <select
                id="bulk-reason"
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                className="field-input"
              >
                <option value="Donated">Donated</option>
                <option value="Disposed">Disposed</option>
                <option value="Archived">Archived</option>
              </select>
            </div>
          )}
          <button type="button" onClick={run} disabled={working} className="button">
            {working ? "Working…" : `${bulk.label} (${selected.size})`}
          </button>
        </div>
      )}

      {message && (
        <p role="status" className="card p-3">
          {message}
        </p>
      )}

      {visible.length === 0 ? (
        <p className="text-ink-muted">Nothing on this page matches the filter.</p>
      ) : (
        <ul className="flex flex-col gap-3">
          {visible.map((item) => (
            <li key={item.id} className="card flex items-start gap-3 px-4 py-3">
              {bulk && (
                <input
                  type="checkbox"
                  aria-label={`Select ${item.title}`}
                  checked={selected.has(item.id)}
                  onChange={() => toggle(item.id)}
                  className="mt-2"
                />
              )}
              <div className="flex-1">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <Link
                    href={`/staff/reports/${item.id}`}
                    className="text-lg font-medium text-accent underline"
                  >
                    {item.title}
                  </Link>
                  <span className="badge">
                    {item.type} · {item.status}
                  </span>
                </div>
                <p className="text-sm text-ink-muted">
                  {item.trackingCode} · {item.categoryName} · {item.locationName} ·{" "}
                  {formatDate(item.occurredOn)}
                  {item.pendingClaims > 0 && ` · ${item.pendingClaims} pending claims`}
                  {item.possibleDuplicateIds.length > 0 &&
                    ` · looks like ${item.possibleDuplicateIds.length} other report(s)`}
                </p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
