import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import * as api from "@/features/tracking/api";
import { ApiError } from "@/lib/api";
import TrackPage from "./page";

afterEach(() => vi.restoreAllMocks());

async function submit() {
  const user = userEvent.setup();
  render(<TrackPage />);
  await user.type(screen.getByLabelText("University ID"), "S12345");
  await user.type(screen.getByLabelText("Tracking code"), "LF-000000");
  await user.click(screen.getByRole("button", { name: /show my report/i }));
}

describe("TrackPage", () => {
  it("answers every failed lookup with the same message", async () => {
    vi.spyOn(api, "trackReport").mockRejectedValue(
      new ApiError(404, {
        title: "Not found.",
        detail: "No report matches this university id and tracking code.",
      }),
    );

    await submit();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "No report matches that university ID and tracking code.",
    );
  });
});
