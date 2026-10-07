import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@/lib/api";
import * as api from "./api";
import { LoginForm } from "./login-form";

const replace = vi.fn();
const refresh = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace, refresh }) }));

afterEach(() => {
  vi.restoreAllMocks();
  // The router mock lives at module scope, so restoreAllMocks does not reset its call log.
  replace.mockClear();
  refresh.mockClear();
});

describe("LoginForm", () => {
  it("sends the credentials and moves on to the queues", async () => {
    const user = userEvent.setup();
    const login = vi.spyOn(api, "login").mockResolvedValue({
      universityId: "ST1001",
      fullName: "Dana Reed",
      expiresAt: "2026-09-21",
    });

    render(<LoginForm />);
    await user.type(screen.getByLabelText("University ID"), "ST1001");
    await user.type(screen.getByLabelText("Password"), "staffdemo123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(login).toHaveBeenCalledWith("ST1001", "staffdemo123");
    expect(replace).toHaveBeenCalledWith("/staff");
    expect(refresh).toHaveBeenCalled();
  });

  it("shows the API's message and keeps what was typed", async () => {
    const user = userEvent.setup();
    vi.spyOn(api, "login").mockRejectedValue(
      new ApiError(401, { detail: "University id or password is incorrect." }),
    );

    render(<LoginForm />);
    await user.type(screen.getByLabelText("University ID"), "ST1001");
    await user.type(screen.getByLabelText("Password"), "wrong");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "University id or password is incorrect.",
    );
    expect(screen.getByLabelText("University ID")).toHaveValue("ST1001");
    expect(replace).not.toHaveBeenCalled();
  });
});
