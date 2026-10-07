import Link from "next/link";

export function Pagination({
  page,
  pageSize,
  totalCount,
  params,
  basePath = "/reports",
}: {
  page: number;
  pageSize: number;
  totalCount: number;
  params: Record<string, string | undefined>;
  basePath?: string;
}) {
  const lastPage = Math.max(1, Math.ceil(totalCount / pageSize));
  if (lastPage === 1) return null;

  const href = (target: number) => {
    const query = new URLSearchParams();
    for (const [key, value] of Object.entries(params)) {
      if (value && key !== "page") query.set(key, value);
    }
    query.set("page", String(target));
    return `${basePath}?${query}`;
  };

  return (
    <nav aria-label="Pagination" className="flex items-center gap-4">
      {page > 1 && (
        <Link href={href(page - 1)} className="text-accent underline">
          Previous
        </Link>
      )}
      <span className="text-sm text-ink-muted">
        Page {page} of {lastPage}
      </span>
      {page < lastPage && (
        <Link href={href(page + 1)} className="text-accent underline">
          Next
        </Link>
      )}
    </nav>
  );
}
