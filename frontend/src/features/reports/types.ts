// Mirrors the backend DTOs. Property names arrive camelCased and enums arrive as strings,
// because Program.cs registers JsonStringEnumConverter.

export type ItemReportType = "Lost" | "Found";

export type ItemReportStatus =
  "PendingHandover" | "Open" | "Matched" | "Returned" | "Closed" | "Cancelled";

export type ItemReportSummary = {
  id: string;
  type: ItemReportType;
  status: ItemReportStatus;
  title: string;
  categoryId: string;
  categoryName: string;
  locationId: string;
  locationName: string;
  occurredOn: string; // DateOnly, "2026-09-18"
};

export type ItemReportDetail = ItemReportSummary & {
  publicDescription: string;
};

export type SecretAnswer = { questionId: string; optionId: string };

export type CreateReport = {
  type: ItemReportType;
  universityId: string;
  categoryId: string;
  title: string;
  publicDescription: string;
  locationId: string;
  occurredOn: string;
  secretDescription: string;
  answers: SecretAnswer[];
  /** Required on a found report, rejected on a lost one: the API decides where the item goes. */
  handoverPointId?: string;
};

export type PossibleDuplicate = {
  id: string;
  title: string;
  occurredOn: string;
  locationId: string;
};

export type CreatedReport = {
  id: string;
  trackingCode: string;
  possibleDuplicates: PossibleDuplicate[];
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
