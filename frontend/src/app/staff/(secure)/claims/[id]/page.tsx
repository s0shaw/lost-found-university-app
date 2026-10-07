import { notFound } from "next/navigation";
import { getStaffClaim } from "@/features/staff/api-server";
import { ClaimComparison } from "@/features/staff/claim-comparison";
import { ApiError } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function StaffClaimPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  let comparison;
  try {
    comparison = await getStaffClaim(id);
  } catch (caught) {
    if (caught instanceof ApiError && caught.status === 404) notFound();
    throw caught;
  }

  return <ClaimComparison comparison={comparison} />;
}
