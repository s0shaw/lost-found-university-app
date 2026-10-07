import Link from "next/link";
import { redirect } from "next/navigation";
import { getSession } from "@/features/staff/api-server";
import { SignOutButton } from "@/features/staff/sign-out-button";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

/** One guard for every page under /staff, so no page repeats the check.
    This is a user experience decision, not a security boundary: the real gate is
    `[Authorize(Roles = "Staff")]` on the API. Redirecting only saves the visitor from an
    empty screen. The login page sits outside this route group, or it would redirect to itself. */
export default async function StaffLayout({ children }: { children: React.ReactNode }) {
  let session;
  try {
    session = await getSession();
  } catch (caught) {
    if (caught instanceof ApiError && caught.status === 401) redirect("/staff/login");
    throw caught;
  }

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-line pb-4">
        <div className="flex flex-wrap items-center gap-2">
          <Link
            href="/staff"
            className="rounded-lg px-3 py-1.5 font-medium text-accent hover:bg-accent-soft"
          >
            Queues
          </Link>
          <Link
            href="/staff/claims"
            className="rounded-lg px-3 py-1.5 font-medium text-accent hover:bg-accent-soft"
          >
            Claims
          </Link>
          <span className="text-sm text-ink-muted">
            Signed in as {session.fullName} ({session.universityId})
          </span>
        </div>
        <SignOutButton />
      </div>
      {children}
    </div>
  );
}
