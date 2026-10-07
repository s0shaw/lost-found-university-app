"use client";

import { useState } from "react";
import { formatDate } from "@/features/reports/report-list";
import { ApiError } from "@/lib/api";
import { cancelReport } from "./api";
import type { TrackedReport, TrackRequest } from "./types";

/** The owner's own view of a report: everything the public page hides, plus the one action
    the owner can still take. */
export function TrackedReport({
  report,
  request,
  onCancelled,
}: {
  report: TrackedReport;
  request: TrackRequest;
  onCancelled: () => void;
}) {
  const [confirming, setConfirming] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // A matched report is a staff decision; the university id and tracking code cannot undo one.
  const cancellable = report.status === "Open" || report.status === "PendingHandover";

  const timeline = [
    ["Handover confirmed", report.handoverConfirmedAt],
    ["Returned to the owner", report.returnedAt],
    // The same timestamp closes a report either way; only the owner's own withdrawal has no reason.
    [closingLabel(report), report.closedAt],
  ].filter(([, at]) => at) as [string, string][];

  async function onCancel() {
    setPending(true);
    setError(null);
    try {
      await cancelReport(request);
      onCancelled();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : "Something went wrong. Try again.");
      setPending(false);
    }
  }

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-3">
        <h1 className="text-3xl font-bold">{report.title}</h1>
        <p className="text-ink-muted">
          {report.trackingCode} · {report.type} · {report.categoryName} · {report.locationName} ·{" "}
          {formatDate(report.occurredOn)}
        </p>
        {/* Status is given in words, never by colour alone. */}
        <p className="font-bold">Status: {report.status}</p>
        <p>{report.publicDescription}</p>
      </div>

      <section className="flex flex-col gap-3 border-t border-line pt-6">
        <h2 className="text-xl font-bold">Only you can see this</h2>
        <p>{report.secretDescription}</p>
        {report.answers.length > 0 && (
          <dl className="flex flex-col gap-2">
            {report.answers.map((answer) => (
              <div key={answer.questionId} className="flex flex-col">
                <dt className="text-sm text-ink-muted">{answer.question}</dt>
                <dd>{answer.answer}</dd>
              </div>
            ))}
          </dl>
        )}
      </section>

      {/* Absent before a claim is approved. Saying "not yet available" would announce that a
          collection point exists at all. */}
      {report.handoverPointName && (
        <section className="flex flex-col gap-3 border-t border-line pt-6">
          <h2 className="text-xl font-bold">Collect it from</h2>
          <p>{report.handoverPointName}</p>
        </section>
      )}

      {timeline.length > 0 && (
        <section className="flex flex-col gap-3 border-t border-line pt-6">
          <h2 className="text-xl font-bold">What happened</h2>
          <ul className="flex flex-col gap-2">
            {timeline.map(([label, at]) => (
              <li key={label}>
                {label} · <span className="text-sm text-ink-muted">{formatTimestamp(at)}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {cancellable && (
        <section className="flex flex-col gap-3 border-t border-line pt-6">
          <h2 className="text-xl font-bold">Found it yourself?</h2>
          {error && (
            <p role="alert" className="text-error">
              {error}
            </p>
          )}
          {/* Two steps instead of window.confirm: a browser modal is hard to reach with a
              keyboard or a screen reader, and it blocks the page while it is open. */}
          {confirming ? (
            <>
              <p>Cancelling is permanent. The report leaves the public list and stays cancelled.</p>
              <div className="flex gap-3">
                <button type="button" onClick={onCancel} disabled={pending} className="button">
                  {pending ? "Cancelling…" : "Yes, cancel it"}
                </button>
                <button
                  type="button"
                  onClick={() => setConfirming(false)}
                  disabled={pending}
                  className="button-secondary"
                >
                  Keep the report
                </button>
              </div>
            </>
          ) : (
            <div>
              <button
                type="button"
                onClick={() => setConfirming(true)}
                className="button-secondary"
              >
                Cancel this report
              </button>
            </div>
          )}
        </section>
      )}
    </div>
  );
}

function closingLabel(report: TrackedReport) {
  if (report.status === "Cancelled") return "Cancelled by you";
  return report.closeReason ? `Closed — ${report.closeReason.toLowerCase()}` : "Closed";
}

/** Timestamps arrive as instants. Rendered in UTC so the same report reads the same way
    wherever it is opened, and so the value never shifts a day like a local-midnight date would. */
function formatTimestamp(iso: string) {
  return new Date(iso).toLocaleString("en-GB", {
    dateStyle: "long",
    timeStyle: "short",
    timeZone: "UTC",
  });
}
