import type { ClaimStatus } from "@/features/claims/types";
import type { PagedResult } from "@/features/reports/types";
import { apiServer } from "@/lib/api-server";
import type {
  StaffClaimComparison,
  StaffClaimSummary,
  StaffQueue,
  StaffReportDetail,
  StaffReportSummary,
  StaffSession,
} from "./types";

// Server components only — `apiServer` reaches into `next/headers`.

export const getSession = () => apiServer<StaffSession>("/api/auth/me");

export function listStaffReports(queue: StaffQueue, page = 1, pageSize = 20) {
  const query = new URLSearchParams({ queue, page: String(page), pageSize: String(pageSize) });
  return apiServer<PagedResult<StaffReportSummary>>(`/api/staff/reports?${query}`);
}

export const getStaffReport = (id: string) =>
  apiServer<StaffReportDetail>(`/api/staff/reports/${id}`);

export function listStaffClaims(status: ClaimStatus = "Pending", page = 1, pageSize = 20) {
  const query = new URLSearchParams({ status, page: String(page), pageSize: String(pageSize) });
  return apiServer<PagedResult<StaffClaimSummary>>(`/api/staff/claims?${query}`);
}

export const getStaffClaim = (id: string) =>
  apiServer<StaffClaimComparison>(`/api/staff/claims/${id}`);
