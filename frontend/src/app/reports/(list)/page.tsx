import { listCategories, listLocations } from "@/features/catalog/api";
import { searchReports } from "@/features/reports/api";
import { Pagination } from "@/features/reports/pagination";
import { ReportFiltersForm } from "@/features/reports/report-filters";
import { ReportList } from "@/features/reports/report-list";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

type SearchParams = Promise<Record<string, string | undefined>>;

export default async function ReportsPage({ searchParams }: { searchParams: SearchParams }) {
  const filters = await searchParams;

  let categories, locations, result;
  let error: string | null = null;
  try {
    [categories, locations, result] = await Promise.all([
      listCategories(),
      listLocations(),
      searchReports(filters),
    ]);
  } catch (caught) {
    // A rejected filter is the visitor's problem and says which filter; anything else is ours.
    // The per-field messages live in `errors`, while `title` is only ever "Validation failed."
    const fieldMessages =
      caught instanceof ApiError ? Object.values(caught.problem.errors ?? {}).flat() : [];
    error =
      fieldMessages.length > 0
        ? fieldMessages.join(" ")
        : "The reports service is unavailable. Try again in a moment.";
  }

  return (
    <div className="flex flex-col gap-8">
      <h1 className="text-3xl font-bold">Reports</h1>

      {error ? (
        <p role="alert" className="text-error">
          {error}
        </p>
      ) : (
        <>
          <ReportFiltersForm categories={categories!} locations={locations!} values={filters} />
          <p className="text-sm text-ink-muted">{result!.totalCount} reports found.</p>
          <ReportList items={result!.items} />
          <Pagination
            page={result!.page}
            pageSize={result!.pageSize}
            totalCount={result!.totalCount}
            params={filters}
          />
        </>
      )}
    </div>
  );
}
