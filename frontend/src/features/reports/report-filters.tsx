import Link from "next/link";
import type { Category, Location } from "@/features/catalog/types";

/** A plain GET form: the selection lands in the query string and the server component fetches
    again. No JavaScript, shareable URL, working back button. */
export function ReportFiltersForm({
  categories,
  locations,
  values,
}: {
  categories: Category[];
  locations: Location[];
  values: Record<string, string | undefined>;
}) {
  return (
    <form method="get" action="/reports" className="flex flex-col gap-4 border-y border-line py-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <label htmlFor="q" className="font-bold">
            Search
          </label>
          <input
            id="q"
            name="q"
            defaultValue={values.q ?? ""}
            className="field-input"
            placeholder="wallet, umbrella…"
          />
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="type" className="font-bold">
            Type
          </label>
          <select id="type" name="type" defaultValue={values.type ?? ""} className="field-input">
            <option value="">Any</option>
            <option value="Lost">Lost</option>
            <option value="Found">Found</option>
          </select>
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="category" className="font-bold">
            Category
          </label>
          <select
            id="category"
            name="category"
            defaultValue={values.category ?? ""}
            className="field-input"
          >
            <option value="">Any</option>
            {categories.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="location" className="font-bold">
            Location
          </label>
          <select
            id="location"
            name="location"
            defaultValue={values.location ?? ""}
            className="field-input"
          >
            <option value="">Any</option>
            {locations.map((location) => (
              <option key={location.id} value={location.id}>
                {location.name}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="from" className="font-bold">
            From
          </label>
          <input
            id="from"
            name="from"
            type="date"
            defaultValue={values.from ?? ""}
            className="field-input"
          />
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="to" className="font-bold">
            To
          </label>
          <input
            id="to"
            name="to"
            type="date"
            defaultValue={values.to ?? ""}
            className="field-input"
          />
        </div>
      </div>

      <div className="flex gap-3">
        <button type="submit" className="button">
          Apply filters
        </button>
        <Link href="/reports" className="button-secondary">
          Clear
        </Link>
      </div>
    </form>
  );
}
