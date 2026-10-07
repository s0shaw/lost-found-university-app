import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueueList } from "./queue-list";
import type { StaffReportSummary } from "./types";

const refresh = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh }) }));

const staffAction = vi.fn();
vi.mock("./api", () => ({ staffAction: (...args: unknown[]) => staffAction(...args) }));

const report = (id: string, title: string, locationName = "Library"): StaffReportSummary => ({
  id,
  trackingCode: `LF-${id}`,
  type: "Found",
  status: "PendingHandover",
  title,
  categoryId: "c1",
  categoryName: "Wallet & Cards",
  locationId: "l1",
  locationName,
  occurredOn: "2026-09-18",
  pendingClaims: 0,
  possibleDuplicateIds: [],
});

const items = [report("1", "Black wallet"), report("2", "Blue umbrella", "Cafeteria")];

describe("QueueList", () => {
  beforeEach(() => {
    refresh.mockClear();
    staffAction.mockReset().mockResolvedValue(undefined);
  });

  it("narrows the page by the text filter", async () => {
    render(<QueueList items={items} bulk={null} />);

    await userEvent.type(screen.getByLabelText("Filter this page"), "cafeteria");

    expect(screen.queryByText("Black wallet")).not.toBeInTheDocument();
    expect(screen.getByText("Blue umbrella")).toBeInTheDocument();
  });

  it("applies the bulk action to every ticked report, then refreshes", async () => {
    render(<QueueList items={items} bulk={{ action: "confirm-handover", label: "Confirm" }} />);

    await userEvent.click(screen.getByLabelText("Select all shown"));
    await userEvent.click(screen.getByRole("button", { name: "Confirm (2)" }));

    await waitFor(() => expect(refresh).toHaveBeenCalled());
    expect(staffAction).toHaveBeenCalledWith("/api/staff/reports/1/confirm-handover", undefined);
    expect(staffAction).toHaveBeenCalledWith("/api/staff/reports/2/confirm-handover", undefined);
    expect(screen.getByRole("status")).toHaveTextContent("Done: 2 reports.");
  });

  it("sends the chosen close reason", async () => {
    render(<QueueList items={items} bulk={{ action: "close", label: "Close", reason: true }} />);

    await userEvent.click(screen.getByLabelText("Select Black wallet"));
    await userEvent.selectOptions(screen.getByLabelText("Close reason"), "Donated");
    await userEvent.click(screen.getByRole("button", { name: "Close (1)" }));

    await waitFor(() => expect(refresh).toHaveBeenCalled());
    expect(staffAction).toHaveBeenCalledWith("/api/staff/reports/1/close", { reason: "Donated" });
  });
});
