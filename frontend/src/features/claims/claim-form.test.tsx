import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { Category, Location } from "@/features/catalog/types";
import type { ItemReportDetail } from "@/features/reports/types";
import * as api from "./api";
import { ClaimForm } from "./claim-form";

const category: Category = {
  id: "c1",
  name: "Wallets & Cards",
  questions: [{ id: "q1", text: "What colour is it?", options: [{ id: "o1", text: "Black" }] }],
};

const handoverPoints: Location[] = [{ id: "l2", name: "Security Desk", isHandoverPoint: true }];

const report: ItemReportDetail = {
  id: "r1",
  type: "Lost",
  status: "Open",
  title: "Black leather wallet",
  categoryId: "c1",
  categoryName: "Wallets & Cards",
  locationId: "l1",
  locationName: "Library",
  occurredOn: "2026-09-18",
  publicDescription: "Lost near the entrance.",
};

afterEach(() => vi.restoreAllMocks());

function renderForm(type: ItemReportDetail["type"]) {
  return render(
    <ClaimForm report={{ ...report, type }} category={category} handoverPoints={handoverPoints} />,
  );
}

describe("ClaimForm", () => {
  it("asks a lost report's claimant where to hand the item in", () => {
    renderForm("Lost");
    expect(screen.getByLabelText(/hand it in/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/when did you lose it/i)).not.toBeInTheDocument();
  });

  it("asks a found report's claimant when they lost it instead", () => {
    renderForm("Found");
    expect(screen.getByLabelText(/when did you lose it/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/hand it in/i)).not.toBeInTheDocument();
  });

  it("sends the handover point only for a lost report", async () => {
    const user = userEvent.setup();
    const createClaim = vi
      .spyOn(api, "createClaim")
      .mockResolvedValue({ id: "cl1", trackingCode: "WXYZ-9876", status: "Pending" });

    renderForm("Lost");
    await user.type(screen.getByLabelText("University ID"), "S12345");
    await user.selectOptions(screen.getByLabelText(/hand it in/i), "l2");
    await user.type(screen.getByLabelText("Identifying details"), "Torn corner");
    await user.selectOptions(screen.getByLabelText("What colour is it?"), "o1");
    await user.click(screen.getByRole("button", { name: /send claim/i }));

    expect(await screen.findByText("WXYZ-9876")).toBeInTheDocument();
    expect(createClaim).toHaveBeenCalledWith({
      reportId: "r1",
      universityId: "S12345",
      secretDescription: "Torn corner",
      answers: [{ questionId: "q1", optionId: "o1" }],
      lostOn: undefined,
      handoverPointId: "l2",
    });
  });
});
