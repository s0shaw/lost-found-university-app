import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import * as api from "./api";
import { TrackedReport } from "./tracked-report";
import type { TrackedReport as TrackedReportData } from "./types";

const request = { universityId: "S12345", trackingCode: "LF-450568" };

const report: TrackedReportData = {
  id: "r1",
  trackingCode: "LF-450568",
  type: "Lost",
  status: "Open",
  title: "Black leather wallet",
  publicDescription: "Lost near the entrance.",
  categoryId: "c1",
  categoryName: "Wallets & Cards",
  locationId: "l1",
  locationName: "Library",
  occurredOn: "2026-09-18",
  secretDescription: "Torn corner on the card slot.",
  answers: [{ questionId: "q1", question: "What colour is it?", optionId: "o1", answer: "Black" }],
  handoverPointName: null,
  handoverConfirmedAt: null,
  returnedAt: null,
  closedAt: null,
  closeReason: null,
};

afterEach(() => vi.restoreAllMocks());

function renderReport(overrides: Partial<TrackedReportData> = {}) {
  return render(
    <TrackedReport report={{ ...report, ...overrides }} request={request} onCancelled={() => {}} />,
  );
}

describe("TrackedReport", () => {
  it("shows the details the public page hides", () => {
    renderReport();
    expect(screen.getByText("Torn corner on the card slot.")).toBeInTheDocument();
    expect(screen.getByText("What colour is it?")).toBeInTheDocument();
    expect(screen.getByText("Black")).toBeInTheDocument();
  });

  it("says nothing about a collection point before one exists", () => {
    renderReport();
    expect(screen.queryByText(/collect it from/i)).not.toBeInTheDocument();
  });

  it("builds the timeline from the timestamps that are set", () => {
    renderReport({ returnedAt: "2026-09-19T09:30:00+00:00" });
    expect(screen.getByText(/returned to the owner/i)).toBeInTheDocument();
    expect(screen.queryByText(/handover confirmed/i)).not.toBeInTheDocument();
    expect(screen.getByText("19 September 2026 at 09:30")).toBeInTheDocument();
  });

  it("names an owner's withdrawal instead of calling it closed", () => {
    renderReport({ status: "Cancelled", closedAt: "2026-09-20T11:37:00+00:00" });
    expect(screen.getByText(/cancelled by you/i)).toBeInTheDocument();
  });

  it("offers cancelling only while the report is still the owner's to withdraw", () => {
    renderReport({ status: "Matched" });
    expect(screen.queryByRole("button", { name: /cancel this report/i })).not.toBeInTheDocument();
  });

  it("cancels in two steps instead of a browser dialog", async () => {
    const user = userEvent.setup();
    const cancel = vi.spyOn(api, "cancelReport").mockResolvedValue(undefined);

    renderReport();
    await user.click(screen.getByRole("button", { name: /cancel this report/i }));
    expect(cancel).not.toHaveBeenCalled();

    await user.click(screen.getByRole("button", { name: /yes, cancel it/i }));
    expect(cancel).toHaveBeenCalledWith(request);
  });
});
