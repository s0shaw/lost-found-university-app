import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ClaimComparison } from "./claim-comparison";
import type { StaffClaimComparison } from "./types";

vi.mock("next/navigation", () => ({ useRouter: () => ({ refresh: vi.fn() }) }));

const comparison: StaffClaimComparison = {
  claim: {
    id: "cl1",
    trackingCode: "CL-080917",
    reportId: "r1",
    reportTitle: "Black leather wallet",
    reportType: "Lost",
    claimantUniversityId: "S12345",
    claimantFullName: "Alex Kaya",
    score: 72,
    status: "Pending",
    source: "User",
    lostOn: "2026-09-18",
    decidedAt: null,
  },
  reportSecretDescription: "Torn corner on the card slot.",
  claimSecretDescription: "The card slot is torn.",
  answers: [
    {
      questionId: "q1",
      question: "What colour is it?",
      reportAnswer: "Black",
      claimAnswer: "Black",
      matches: true,
    },
    {
      questionId: "q2",
      question: "Any marks?",
      reportAnswer: "Scratched corner",
      claimAnswer: "I don't know",
      matches: false,
    },
  ],
  score: 72,
  matchedAnswers: 1,
  totalAnswers: 2,
  textOverlap: 0.5,
  sharedWords: ["card", "slot", "torn"],
  sameLocation: null,
  timelineConflict: false,
  handoverPointName: null,
};

function renderComparison(overrides: Partial<StaffClaimComparison> = {}) {
  return render(<ClaimComparison comparison={{ ...comparison, ...overrides }} />);
}

const paragraph = (text: string) =>
  screen.getByText((_, element) => element?.tagName === "P" && element.textContent === text);

describe("ClaimComparison", () => {
  it("puts the two secret descriptions side by side", () => {
    renderComparison();
    // Shared words are wrapped in <mark>, so the text sits across several child nodes.
    expect(paragraph("Torn corner on the card slot.")).toBeInTheDocument();
    expect(paragraph("The card slot is torn.")).toBeInTheDocument();
  });

  it("marks the words the two descriptions share", () => {
    const { container } = renderComparison();
    const marked = [...container.querySelectorAll("mark")].map((mark) => mark.textContent);
    expect(marked).toEqual(["Torn", "card", "slot.", "card", "slot", "torn."]);
  });

  it("prints the API's verdict per answer in words", () => {
    renderComparison();
    const rows = screen.getAllByRole("row");
    expect(rows[1]).toHaveTextContent("match");
    expect(rows[2]).not.toHaveTextContent("match");
  });

  it("separates a claim with no location to compare from a mismatched one", () => {
    renderComparison();
    expect(screen.getByText("not comparable")).toBeInTheDocument();
  });

  it("shows the handover point only when the API sends one", () => {
    const { unmount } = renderComparison();
    expect(screen.queryByText("Handover point")).not.toBeInTheDocument();
    unmount();

    renderComparison({ handoverPointName: "Library Info Desk" });
    expect(screen.getByText("Library Info Desk")).toBeInTheDocument();
  });

  it("offers a decision while the claim is pending", () => {
    renderComparison();
    expect(screen.getByRole("button", { name: /approve claim/i })).toBeInTheDocument();
  });

  it("states the outcome instead of the buttons once the claim is decided", () => {
    renderComparison({
      claim: { ...comparison.claim, status: "Rejected", decidedAt: "2026-09-19T09:30:00+00:00" },
    });
    expect(screen.getByText(/already rejected/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /approve claim/i })).not.toBeInTheDocument();
  });

  it("asks for a note before it will send a rejection", async () => {
    const user = userEvent.setup();
    renderComparison();
    expect(screen.queryByLabelText(/why is it being rejected/i)).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /reject claim/i }));
    expect(screen.getByLabelText(/why is it being rejected/i)).toBeRequired();
  });
});
