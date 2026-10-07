import Link from "next/link";
import { searchReports } from "@/features/reports/api";
import { ReportList } from "@/features/reports/report-list";

export const dynamic = "force-dynamic";

export default async function HomePage() {
  // null, not an empty array: "nothing reported yet" and "the API is down" are different
  // things and the visitor is told which one they are looking at.
  let recent = null;
  try {
    recent = await searchReports({ pageSize: 5 });
  } catch {
    recent = null;
  }

  return (
    <div className="flex flex-col gap-8">
      <div className="hero flex flex-col gap-3 p-6 sm:p-8">
        <h1 className="text-3xl font-extrabold tracking-tight">University Lost &amp; Found</h1>
        <p className="opacity-90">
          Report an item you lost or found on university. Staff match the two and hand the item back
          at a collection point.
        </p>
        <div className="mt-2 flex flex-wrap gap-3">
          <Link href="/reports/new?type=lost" className="button">
            I lost something
          </Link>
          <Link href="/reports/new?type=found" className="button-secondary">
            I found something
          </Link>
        </div>
      </div>

      <section className="flex flex-col gap-4">
        <h2 className="text-xl font-bold">Recent reports</h2>
        {recent === null ? (
          <p role="alert" className="text-error">
            The reports service is unavailable. Try again in a moment.
          </p>
        ) : (
          <ReportList items={recent.items} />
        )}
        <Link href="/reports" className="text-accent underline">
          See all reports
        </Link>
      </section>
    </div>
  );
}
