import { api } from "@/lib/api";
import type { TrackedReport, TrackRequest } from "./types";

export const trackReport = (request: TrackRequest) =>
  api<TrackedReport>("/api/track", { method: "POST", body: JSON.stringify(request) });

export const cancelReport = (request: TrackRequest) =>
  api<void>("/api/track/cancel", { method: "POST", body: JSON.stringify(request) });
