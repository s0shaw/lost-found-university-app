import Link from "next/link";
import { formatDate } from "./report-list";
import type { CreatedReport } from "./types";

/** Replaces the form once the report exists. The tracking code is the one thing the visitor
    cannot get back on their own, so it is the loudest thing on the page. */
export function TrackingCode({ created }: { created: CreatedReport }) {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-bold">Report created</h1>
      <div className="border border-ink p-4">
        <p className="text-sm text-ink-muted">Your tracking code</p>
        <p className="text-3xl font-bold">{created.trackingCode}</p>
      </div>
      <p>
        Write this down. With your university ID it is the only way to see your report, including
        the details that stay hidden from everyone else.
      </p>

      {created.possibleDuplicates.length > 0 && (
        <section className="flex flex-col gap-3 border-t border-line pt-6">
          <h2 className="text-xl font-bold">These reports look similar</h2>
          {/* A warning, never a block — the report is already created, exactly as the API decided. */}
          <p className="text-sm text-ink-muted">
            Your report was created. If one of these is the same item, you can claim it instead.
          </p>
          <ul className="flex flex-col gap-2">
            {created.possibleDuplicates.map((duplicate) => (
              <li key={duplicate.id}>
                <Link href={`/reports/${duplicate.id}`} className="text-accent underline">
                  {duplicate.title}
                </Link>{" "}
                <span className="text-sm text-ink-muted">{formatDate(duplicate.occurredOn)}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {/* No link to the public page: a found report waits for handover and answers 404 there
          until staff confirm the item arrived. */}
      <div>
        <Link href="/track" className="button">
          Track this report
        </Link>
      </div>
    </div>
  );
}
