import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ReportList, formatDate } from "./report-list";
import type { ItemReportSummary } from "./types";

const item: ItemReportSummary = {
  id: "5f2b1c9e-0000-4000-8000-000000000001",
  type: "Lost",
  status: "Open",
  title: "Black leather wallet",
  categoryId: "c1",
  categoryName: "Wallets & Cards",
  locationId: "l1",
  locationName: "Library",
  occurredOn: "2026-09-18",
};

describe("ReportList", () => {
  it("says so instead of rendering an empty list", () => {
    render(<ReportList items={[]} />);
    expect(screen.getByText(/no reports match/i)).toBeInTheDocument();
  });

  it("links each report to its detail page", () => {
    render(<ReportList items={[item]} />);
    expect(screen.getByRole("link", { name: item.title })).toHaveAttribute(
      "href",
      `/reports/${item.id}`,
    );
  });

  it("spells out the type and status rather than relying on colour", () => {
    render(<ReportList items={[item]} />);
    expect(screen.getByText("Lost · Open")).toBeInTheDocument();
  });
});

describe("formatDate", () => {
  it("does not shift the date across time zones", () => {
    expect(formatDate("2026-01-01")).toBe("1 January 2026");
    expect(formatDate("2026-09-18")).toBe("18 September 2026");
  });
});
