import { api } from "@/lib/api";
import type { StaffSession } from "./types";

// Browser-side calls only: the cookie rides along because the rewrite keeps the API same-origin.
// Server components read through `api-server.ts` instead, which forwards the cookie by hand.

export const login = (universityId: string, password: string) =>
  api<StaffSession>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ universityId, password }),
  });

export const logout = () => api<void>("/api/auth/logout", { method: "POST" });

/** Every staff decision is the same shape: POST, no response body, then reread the page.
    The endpoint is a plain string so a server component can hand it to the action button. */
export const staffAction = (endpoint: string, body?: unknown) =>
  api<void>(endpoint, {
    method: "POST",
    body: body === undefined ? undefined : JSON.stringify(body),
  });
