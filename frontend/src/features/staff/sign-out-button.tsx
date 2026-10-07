"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { logout } from "./api";

export function SignOutButton() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function onClick() {
    setPending(true);
    // Clearing the cookie is the whole sign-out: the token is short lived and has no refresh.
    await logout().catch(() => {});
    router.replace("/staff/login");
    router.refresh();
  }

  return (
    <button type="button" onClick={onClick} disabled={pending} className="button-secondary">
      {pending ? "Signing out…" : "Sign out"}
    </button>
  );
}
