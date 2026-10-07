import { notFound } from "next/navigation";
import { ClaimForm } from "@/features/claims/claim-form";
import { listCategories, listLocations } from "@/features/catalog/api";
import { getReport } from "@/features/reports/api";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function ClaimPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  let report, categories, locations;
  try {
    [report, categories, locations] = await Promise.all([
      getReport(id),
      listCategories(),
      listLocations(),
    ]);
  } catch (caught) {
    if (caught instanceof ApiError && caught.status === 404) notFound();
    throw caught;
  }

  // The claim answers the report's own questions, so the category is not the claimant's to pick.
  const category = categories.find((candidate) => candidate.id === report.categoryId);
  if (!category) notFound();

  return (
    <ClaimForm
      report={report}
      category={category}
      handoverPoints={locations.filter((location) => location.isHandoverPoint)}
    />
  );
}
