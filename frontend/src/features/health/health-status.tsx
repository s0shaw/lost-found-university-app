import { apiText } from "@/lib/api";

/** Server component: calls the backend /health endpoint and renders the result. */
export async function HealthStatus() {
  let status = "unreachable";
  try {
    status = await apiText("/health");
  } catch {
    // Backend down or misconfigured API_URL: show it, do not crash the page.
  }
  const healthy = status === "Healthy";
  // The word carries the meaning; the colour only repeats it, so a colour-blind reader loses nothing.
  return (
    <p data-testid="health-status" className={healthy ? undefined : "text-error"}>
      Backend: {status}
    </p>
  );
}
