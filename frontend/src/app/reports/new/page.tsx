import { notFound } from "next/navigation";
import { listCategories, listLocations } from "@/features/catalog/api";
import { ReportForm } from "@/features/reports/report-form";
import type { ItemReportType } from "@/features/reports/types";

export const dynamic = "force-dynamic";

export default async function NewReportPage({
  searchParams,
}: {
  searchParams: Promise<{ type?: string }>;
}) {
  const { type } = await searchParams;
  // The two forms differ enough that "which one" is part of the address, and nothing else is.
  if (type !== "lost" && type !== "found") notFound();

  const [categories, locations] = await Promise.all([listCategories(), listLocations()]);
  const reportType: ItemReportType = type === "lost" ? "Lost" : "Found";

  return <ReportForm type={reportType} categories={categories} locations={locations} />;
}
