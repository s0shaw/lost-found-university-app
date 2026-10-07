import { api } from "@/lib/api";
import type {
  CreateReport,
  CreatedReport,
  ItemReportDetail,
  ItemReportSummary,
  PagedResult,
} from "./types";

/** Whatever arrived in the query string. The API validates it and answers 400 on nonsense,
    so the page passes it through rather than guarding every value twice. */
export type ReportFilters = Record<string, string | number | undefined>;

export function searchReports(filters: ReportFilters = {}) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value !== undefined && value !== "") query.set(key, String(value));
  }
  const suffix = query.size > 0 ? `?${query}` : "";
  return api<PagedResult<ItemReportSummary>>(`/api/reports${suffix}`);
}

export const getReport = (id: string) => api<ItemReportDetail>(`/api/reports/${id}`);

export const createReport = (report: CreateReport) =>
  api<CreatedReport>("/api/reports", { method: "POST", body: JSON.stringify(report) });
