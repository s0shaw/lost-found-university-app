import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { Category, Location } from "@/features/catalog/types";
import { ApiError } from "@/lib/api";
import * as api from "./api";
import { ReportForm } from "./report-form";

const categories: Category[] = [
  {
    id: "c1",
    name: "Wallets & Cards",
    questions: [
      {
        id: "q1",
        text: "What colour is it?",
        options: [{ id: "o1", text: "Black" }],
      },
    ],
  },
];

const locations: Location[] = [
  { id: "l1", name: "Library", isHandoverPoint: false },
  { id: "l2", name: "Security Desk", isHandoverPoint: true },
];

afterEach(() => vi.restoreAllMocks());

async function fillRequiredFields(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText("University ID"), "S12345");
  await user.type(screen.getByLabelText("Title"), "Black leather wallet");
  await user.selectOptions(screen.getByLabelText("Category"), "c1");
  await user.selectOptions(screen.getByLabelText(/where did you lose it/i), "l1");
  await user.type(screen.getByLabelText(/when did you lose it/i), "2026-09-18");
  await user.type(screen.getByLabelText("Identifying details"), "Torn corner");
  await user.selectOptions(screen.getByLabelText("What colour is it?"), "o1");
}

describe("ReportForm", () => {
  it("only asks a found report where the item will be handed in", () => {
    const { rerender } = render(
      <ReportForm type="Lost" categories={categories} locations={locations} />,
    );
    expect(screen.queryByLabelText(/hand it in/i)).not.toBeInTheDocument();

    rerender(<ReportForm type="Found" categories={categories} locations={locations} />);
    // Only collection points, not every location on university.
    const handover = screen.getByLabelText(/hand it in/i);
    expect(within(handover).getByRole("option", { name: "Security Desk" })).toBeInTheDocument();
    expect(within(handover).queryByRole("option", { name: "Library" })).not.toBeInTheDocument();
  });

  it("shows the tracking code instead of the form once the report exists", async () => {
    const user = userEvent.setup();
    vi.spyOn(api, "createReport").mockResolvedValue({
      id: "r1",
      trackingCode: "ABCD-1234",
      possibleDuplicates: [],
    });

    render(<ReportForm type="Lost" categories={categories} locations={locations} />);
    await fillRequiredFields(user);
    await user.click(screen.getByRole("button", { name: /create report/i }));

    expect(await screen.findByText("ABCD-1234")).toBeInTheDocument();
    expect(screen.queryByLabelText("University ID")).not.toBeInTheDocument();
  });

  it("puts a field error next to its field", async () => {
    const user = userEvent.setup();
    vi.spyOn(api, "createReport").mockRejectedValue(
      new ApiError(400, { title: "Validation failed.", errors: { UniversityId: ["Unknown ID."] } }),
    );

    render(<ReportForm type="Lost" categories={categories} locations={locations} />);
    await fillRequiredFields(user);
    await user.click(screen.getByRole("button", { name: /create report/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Unknown ID.");
    expect(screen.getByLabelText("University ID")).toHaveAttribute("aria-invalid", "true");
  });

  it("falls back to the form-level message when no field matches", async () => {
    const user = userEvent.setup();
    vi.spyOn(api, "createReport").mockRejectedValue(
      new ApiError(400, { detail: "The university ID has expired." }),
    );

    render(<ReportForm type="Lost" categories={categories} locations={locations} />);
    await fillRequiredFields(user);
    await user.click(screen.getByRole("button", { name: /create report/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent("The university ID has expired.");
  });
});
