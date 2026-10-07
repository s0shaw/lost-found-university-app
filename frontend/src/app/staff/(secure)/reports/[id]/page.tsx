import Link from "next/link";
import { notFound } from "next/navigation";
import { formatDate } from "@/features/reports/report-list";
import { getStaffClaim, getStaffReport } from "@/features/staff/api-server";
import { ClaimDetails } from "@/features/staff/claim-comparison";
import { StaffActionButton } from "@/features/staff/staff-action-button";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function StaffReportPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  let detail;
  try {
    detail = await getStaffReport(id);
  } catch (caught) {
    if (caught instanceof ApiError && caught.status === 404) notFound();
    throw caught;
  }

  const { report, candidates } = detail;
  // Strongest claim first. Pending claims are fetched here so the decision sits on this page.
  const claims = [...detail.claims].sort((a, b) => b.score - a.score);
  const comparisons = new Map(
    await Promise.all(
      claims
        .filter((claim) => claim.status === "Pending")
        .map(async (claim) => [claim.id, await getStaffClaim(claim.id)] as const),
    ),
  );
  const firstPendingId = claims.find((claim) => claim.status === "Pending")?.id;
  const finished =
    report.status === "Returned" || report.status === "Closed" || report.status === "Cancelled";

  return (
    <article className="flex flex-col gap-8">
      <div className="flex flex-col gap-2">
        <p className="text-sm text-ink-muted">
          {report.trackingCode} · {report.type} · {report.status}
        </p>
        <h1 className="text-3xl font-bold">{report.title}</h1>
      </div>

      {/* Only the actions the report's status allows: the API rejects the rest, and offering a
          button that always fails is a promise the screen cannot keep. */}
      {!finished && (
        <section className="card flex flex-col gap-3 p-4">
          <h2 className="text-lg font-bold">Report actions</h2>
          <div className="flex flex-wrap items-start gap-3">
            {report.status === "PendingHandover" && (
              <StaffActionButton
                endpoint={`/api/staff/reports/${report.id}/confirm-handover`}
                label="Confirm handover"
                confirm="The item is at its collection point and the report becomes public."
              />
            )}
            {report.status === "Matched" && (
              <StaffActionButton
                endpoint={`/api/staff/reports/${report.id}/mark-returned`}
                label="Mark returned"
                confirm="The owner has the item back."
              />
            )}
            <StaffActionButton
              endpoint={`/api/staff/reports/${report.id}/close`}
              label="Close report"
              field="reason"
              secondary
            />
            <StaffActionButton
              endpoint={`/api/staff/reports/${report.id}/cancel`}
              label="Cancel report"
              confirm="Cancelling is permanent and rejects every pending claim."
              secondary
            />
          </div>
        </section>
      )}

      <dl className="grid gap-3 border-y border-line py-6 sm:grid-cols-[12rem_1fr]">
        <dt className="font-bold">Reported by</dt>
        <dd>
          {report.reporterFullName} ({report.reporterUniversityId})
        </dd>
        <dt className="font-bold">Category</dt>
        <dd>{report.categoryName}</dd>
        <dt className="font-bold">Location</dt>
        <dd>{report.locationName}</dd>
        <dt className="font-bold">Date</dt>
        <dd>{formatDate(report.occurredOn)}</dd>
        <dt className="font-bold">Public description</dt>
        <dd>{report.publicDescription || "—"}</dd>
        <dt className="font-bold">Identifying details</dt>
        <dd>{report.secretDescription}</dd>
        {report.handoverPointName && (
          <>
            <dt className="font-bold">Collection point</dt>
            <dd>{report.handoverPointName}</dd>
          </>
        )}
      </dl>

      {report.answers.length > 0 && (
        <section className="flex flex-col gap-3">
          <h2 className="text-xl font-bold">The owner&apos;s answers</h2>
          <dl className="flex flex-col gap-2">
            {report.answers.map((answer) => (
              <div key={answer.questionId} className="flex flex-col">
                <dt className="text-sm text-ink-muted">{answer.question}</dt>
                <dd>{answer.answer}</dd>
              </div>
            ))}
          </dl>
        </section>
      )}

      <section className="flex flex-col gap-3 border-t border-line pt-6">
        <h2 className="text-xl font-bold">Claims</h2>
        {claims.length === 0 ? (
          <p className="text-ink-muted">Nobody has claimed this report.</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {claims.map((claim) => {
              const comparison = comparisons.get(claim.id);
              return (
                <li key={claim.id} className="card px-4 py-3">
                  {comparison ? (
                    <details open={claim.id === firstPendingId}>
                      <summary className="flex cursor-pointer flex-wrap items-baseline justify-between gap-2">
                        <span className="font-medium">
                          {claim.claimantFullName} ({claim.claimantUniversityId})
                        </span>
                        <span className="badge">
                          Score {claim.score} · {claim.status}
                        </span>
                      </summary>
                      <div className="pt-4">
                        <ClaimDetails comparison={comparison} />
                      </div>
                    </details>
                  ) : (
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <Link href={`/staff/claims/${claim.id}`} className="text-accent underline">
                        {claim.claimantFullName} ({claim.claimantUniversityId})
                      </Link>
                      <span className="badge">
                        Score {claim.score} · {claim.status}
                      </span>
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </section>

      {candidates.length > 0 && (
        <section className="flex flex-col gap-3 border-t border-line pt-6">
          <h2 className="text-xl font-bold">Reports that look like this one</h2>
          <ul className="flex flex-col gap-3">
            {candidates.map((candidate) => (
              <li key={candidate.id} className="card px-4 py-3">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <Link href={`/staff/reports/${candidate.id}`} className="text-accent underline">
                    {candidate.title}
                  </Link>
                  <span className="text-sm">
                    Score {candidate.score} · {candidate.type}
                  </span>
                </div>
                <p className="text-sm text-ink-muted">
                  {candidate.trackingCode} · {formatDate(candidate.occurredOn)}
                </p>
              </li>
            ))}
          </ul>
        </section>
      )}
    </article>
  );
}
