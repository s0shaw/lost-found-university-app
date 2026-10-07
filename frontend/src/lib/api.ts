/**
 * Thin fetch wrapper around the backend API.
 * Errors follow RFC 9457 ProblemDetails (see backend ApiExceptionHandler).
 */
// The browser always calls this site and lets the next.config rewrite forward to the API, so the
// staff session cookie stays first-party. Server components have no rewrite in front of them and
// reach the API directly through API_URL.
const baseUrl =
  typeof window === "undefined" ? (process.env.API_URL ?? "http://localhost:5000") : "";

export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails,
  ) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
  }
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...init?.headers },
    cache: "no-store",
  });

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

export async function apiText(path: string): Promise<string> {
  const response = await fetch(`${baseUrl}${path}`, { cache: "no-store" });
  if (!response.ok) {
    throw new ApiError(response.status, { title: `Request failed with status ${response.status}` });
  }
  return response.text();
}
