"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { Field } from "@/components/field";
import { ApiError } from "@/lib/api";
import { login } from "./api";

export function LoginForm() {
  const router = useRouter();
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setPending(true);
    setErrors({});
    try {
      await login(String(data.get("universityId") ?? ""), String(data.get("password") ?? ""));
      // The session lives in a cookie the server reads, so the cached server tree is stale
      // until it is refreshed; without this the guard in the layout would still see a 401.
      router.replace("/staff");
      router.refresh();
    } catch (caught) {
      setErrors(
        caught instanceof ApiError
          ? (caught.problem.errors ?? { form: [caught.message] })
          : { form: ["Something went wrong. Try again."] },
      );
      setPending(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-8">
      <div className="flex flex-col gap-3">
        <h1 className="text-3xl font-bold">Staff sign in</h1>
        <p className="text-ink-muted">
          Handover, claim decisions and the queues live behind this form.
        </p>
      </div>

      {errors.form && (
        <p role="alert" className="text-error">
          {errors.form.join(" ")}
        </p>
      )}

      <Field id="universityId" label="University ID" errors={errors.UniversityId}>
        {(props) => (
          <input
            {...props}
            name="universityId"
            required
            autoComplete="username"
            className="field-input"
          />
        )}
      </Field>

      <Field id="password" label="Password" errors={errors.Password}>
        {(props) => (
          <input
            {...props}
            name="password"
            type="password"
            required
            autoComplete="current-password"
            className="field-input"
          />
        )}
      </Field>

      <div>
        <button type="submit" disabled={pending} className="button">
          {pending ? "Signing in…" : "Sign in"}
        </button>
      </div>
    </form>
  );
}
