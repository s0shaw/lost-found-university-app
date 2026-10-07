import type { ItemReportStatus, ItemReportType } from "@/features/reports/types";

/** The identity pair travels in the body, not the query string: a tracking code in a URL ends
    up in browser history, proxies and server logs. */
export type TrackRequest = { universityId: string; trackingCode: string };

export type ReportCloseReason = "Donated" | "Disposed" | "Archived";

export type TrackedAnswer = {
  questionId: string;
  question: string | null;
  optionId: string;
  answer: string | null;
};

export type TrackedReport = {
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
  /** Owner-only, next to the answers: this view is the one place either is readable. */
  secretDescription: string;
  answers: TrackedAnswer[];
  /** Filled once staff approve a claim, null before that. */
  handoverPointName: string | null;
  handoverConfirmedAt: string | null;
  returnedAt: string | null;
  closedAt: string | null;
  closeReason: ReportCloseReason | null;
};
