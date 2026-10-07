import Link from "next/link";
import type { ClaimStatus } from "@/features/claims/types";
import { Pagination } from "@/features/reports/pagination";
import { listStaffClaims } from "@/features/staff/api-server";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

const statuses: ClaimStatus[] = ["Pending", "Approved", "Rejected", "Withdrawn"];

function isStatus(value: string | undefined): value is ClaimStatus {
  return statuses.includes(value as ClaimStatus);
}

type SearchParams = Promise<Record<string, string | undefined>>;

export default async function StaffClaimsPage({ searchParams }: { searchParams: SearchParams }) {
  const params = await searchParams;
  const status = isStatus(params.status) ? params.status : "Pending";

  let result;
  let error: string | null = null;
  try {
    result = await listStaffClaims(status, Number(params.page) || 1);
  } catch (caught) {
    const fieldMessages =
      caught instanceof ApiError ? Object.values(caught.problem.errors ?? {}).flat() : [];
    error =
      fieldMessages.length > 0
        ? fieldMessages.join(" ")
        : "The claims service is unavailable. Try again in a moment.";
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-bold">Claims</h1>

      <nav aria-label="Claim status">
        <ul className="flex flex-wrap gap-2">
          {statuses.map((item) => (
            <li key={item}>
              <Link
                href={`/staff/claims?status=${item}`}
                aria-current={item === status ? "page" : undefined}
                className={`badge px-4 py-1 text-base ${item === status ? "bg-accent! text-page!" : ""}`}
              >
                {item}
              </Link>
            </li>
          ))}
        </ul>
      </nav>

      {error ? (
        <p role="alert" className="text-error">
          {error}
        </p>
      ) : result!.items.length === 0 ? (
        <p className="text-ink-muted">No {status.toLowerCase()} claims.</p>
      ) : (
        <>
          <p className="text-sm text-ink-muted">
            {result!.totalCount} {result!.totalCount === 1 ? "claim" : "claims"}. The API sorts
            pending claims by score, strongest first.
          </p>
          <ul className="flex flex-col gap-3">
            {result!.items.map((claim) => (
              <li key={claim.id} className="card px-4 py-3">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <Link
                    href={`/staff/claims/${claim.id}`}
                    className="text-lg text-accent underline"
                  >
                    {claim.reportTitle}
                  </Link>
                  <span className="badge">
                    Score {claim.score} · {claim.status}
                  </span>
                </div>
                <p className="text-sm text-ink-muted">
                  {claim.trackingCode} · {claim.reportType} report · claimed by{" "}
                  {claim.claimantFullName} ({claim.claimantUniversityId})
                </p>
              </li>
            ))}
          </ul>
          <Pagination
            page={result!.page}
            pageSize={result!.pageSize}
            totalCount={result!.totalCount}
            params={{ status }}
            basePath="/staff/claims"
          />
        </>
      )}
    </div>
  );
}
