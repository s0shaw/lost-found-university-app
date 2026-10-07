import type { ClaimStatus } from "@/features/claims/types";
import type { ItemReportStatus, ItemReportType } from "@/features/reports/types";
import type { ReportCloseReason, TrackedAnswer } from "@/features/tracking/types";

// Mirrors the backend staff DTOs (Application/Staff). Enums arrive as strings.

/** No token: it stays in the httpOnly cookie, so the page asks the API who it is. */
export type StaffSession = { universityId: string; fullName: string; expiresAt: string };

export type StaffQueue = "PendingHandover" | "Stale" | "PossibleDuplicate";

export type ClaimSource = "User" | "StaffSuggested";

export type StaffReportSummary = {
  id: string;
  trackingCode: string;
  type: ItemReportType;
  status: ItemReportStatus;
  title: string;
  categoryId: string;
  categoryName: string;
  locationId: string;
  locationName: string;
  occurredOn: string;
  pendingClaims: number;
  /** Filled by the duplicate queue only; empty everywhere else. */
  possibleDuplicateIds: string[];
};

export type StaffClaimSummary = {
  id: string;
  trackingCode: string;
  reportId: string;
  reportTitle: string;
  reportType: ItemReportType;
  claimantUniversityId: string;
  claimantFullName: string;
  score: number;
  status: ClaimStatus;
  source: ClaimSource;
  lostOn: string | null;
  decidedAt: string | null;
};

/** Everything the public and owner views hide, including who reported it. */
export type StaffItemReport = {
  id: string;
  trackingCode: string;
  type: ItemReportType;
  status: ItemReportStatus;
  title: string;
  publicDescription: string;
  categoryId: string;
  categoryName: string;
  locationId: string;
  locationName: string;
  occurredOn: string;
  secretDescription: string;
  answers: TrackedAnswer[];
  handoverPointName: string | null;
  reporterUniversityId: string;
  reporterFullName: string;
  handoverConfirmedAt: string | null;
  returnedAt: string | null;
  closedAt: string | null;
  closeReason: ReportCloseReason | null;
};

export type CandidateReport = {
  id: string;
  trackingCode: string;
  type: ItemReportType;
  title: string;
  occurredOn: string;
  score: number;
};

export type StaffReportDetail = {
  report: StaffItemReport;
  claims: StaffClaimSummary[];
  candidates: CandidateReport[];
};

/** `matches` is decided by the API. The comparison screen prints it, it does not recompute it. */
export type AnswerComparison = {
  questionId: string;
  question: string | null;
  reportAnswer: string | null;
  claimAnswer: string | null;
  matches: boolean;
};

export type StaffClaimComparison = {
  claim: StaffClaimSummary;
  reportSecretDescription: string;
  claimSecretDescription: string;
  answers: AnswerComparison[];
  score: number;
  matchedAnswers: number;
  totalAnswers: number;
  textOverlap: number;
  sharedWords: string[];
  sameLocation: boolean | null;
  timelineConflict: boolean;
  handoverPointName: string | null;
};
