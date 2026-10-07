import type { SecretAnswer } from "@/features/reports/types";

export type ClaimStatus = "Pending" | "Approved" | "Rejected" | "Withdrawn";

export type CreateClaim = {
  reportId: string;
  universityId: string;
  secretDescription: string;
  answers: SecretAnswer[];
  /** Feeds the match score only; the API accepts a claim without it. */
  lostOn?: string;
  /** Claiming a lost report means "I have it", so the claimant picks the drop-off point.
      A claim on a found report must not send one. */
  handoverPointId?: string;
};

export type CreatedClaim = {
  id: string;
  trackingCode: string;
  status: ClaimStatus;
};
