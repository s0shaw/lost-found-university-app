import { api } from "@/lib/api";
import type { CreateClaim, CreatedClaim } from "./types";

export const createClaim = (claim: CreateClaim) =>
  api<CreatedClaim>("/api/claims", { method: "POST", body: JSON.stringify(claim) });
