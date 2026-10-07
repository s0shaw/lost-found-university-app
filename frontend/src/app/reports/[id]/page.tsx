import Link from "next/link";
import { notFound } from "next/navigation";
import { getReport } from "@/features/reports/api";
import { formatDate } from "@/features/reports/report-list";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function ReportDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  let report;
  try {
    report = await getReport(id);
  } catch (caught) {
    // Reports awaiting handover answer 404 as well: the possession gate lives in the API,
    // and this page deliberately does not reimplement it.
    if (caught instanceof ApiError && caught.status === 404) notFound();
    throw caught;
  }

  return (
    <article className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-sm text-ink-muted">
          {report.type} · {report.status}
        </p>
        <h1 className="text-3xl font-bold">{report.title}</h1>
      </div>

      <dl className="grid gap-3 border-y border-line py-6 sm:grid-cols-[10rem_1fr]">
        <dt className="font-bold">Category</dt>
        <dd>{report.categoryName}</dd>
        <dt className="font-bold">Location</dt>
        <dd>{report.locationName}</dd>
        <dt className="font-bold">Date</dt>
        <dd>{formatDate(report.occurredOn)}</dd>
        <dt className="font-bold">Description</dt>
        <dd>{report.publicDescription || "—"}</dd>
      </dl>

      <p className="text-sm text-ink-muted">
        Identifying details are deliberately not shown. You are asked for them when you make a
        claim, and staff compare your answers with the ones on this report.
      </p>

      <div>
        <Link href={`/reports/${report.id}/claim`} className="button">
          {report.type === "Lost" ? "I found this item" : "This item is mine"}
        </Link>
      </div>
    </article>
  );
}
