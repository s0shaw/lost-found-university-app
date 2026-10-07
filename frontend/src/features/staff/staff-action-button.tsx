"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { Field } from "@/components/field";
import { ApiError } from "@/lib/api";
import { staffAction } from "./api";

/** The six staff decisions are one shape: POST, then reread the page rather than patch the
    screen locally, so what is shown is what the API stored.
    Two of them carry a value — a close reason, a rejection note — which is why this is a form
    and not a bare button. */
export function StaffActionButton({
  endpoint,
  label,
  field,
  confirm,
  secondary,
}: {
  endpoint: string;
  label: string;
  field?: "reason" | "staffNote";
  /** When set, the button opens this sentence and a second click sends. No window.confirm:
      a browser modal is hard to reach with a keyboard and blocks the page while open. */
  confirm?: string;
  secondary?: boolean;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, setPending] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  const needsStep = Boolean(confirm) || Boolean(field);
  const id = `${endpoint}-${field ?? "action"}`;

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setPending(true);
    setErrors([]);
    try {
      await staffAction(endpoint, field ? { [field]: String(data.get(field) ?? "") } : undefined);
      setOpen(false);
      router.refresh();
    } catch (caught) {
      const problem = caught instanceof ApiError ? caught.problem : undefined;
      const fieldMessages = Object.values(problem?.errors ?? {}).flat();
      setErrors(
        fieldMessages.length > 0
          ? fieldMessages
          : [caught instanceof ApiError ? caught.message : "Something went wrong. Try again."],
      );
    } finally {
      setPending(false);
    }
  }

  if (needsStep && !open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className={secondary ? "button-secondary" : "button"}
      >
        {label}
      </button>
    );
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3">
      {errors.length > 0 && (
        <p role="alert" className="text-error">
          {errors.join(" ")}
        </p>
      )}

      {confirm && <p>{confirm}</p>}

      {field === "reason" && (
        <Field id={id} label="Why is it being closed?">
          {(props) => (
            <select {...props} name="reason" required className="field-input">
              <option value="Donated">Donated</option>
              <option value="Disposed">Disposed</option>
              <option value="Archived">Archived</option>
            </select>
          )}
        </Field>
      )}

      {field === "staffNote" && (
        <Field
          id={id}
          label="Why is it being rejected?"
          hint="Required. A rejection without a reason is a decision nobody can review later."
        >
          {(props) => (
            <textarea {...props} name="staffNote" rows={3} required className="field-input" />
          )}
        </Field>
      )}

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={pending}
          className={secondary ? "button-secondary" : "button"}
        >
          {pending ? "Working…" : label}
        </button>
        {needsStep && (
          <button
            type="button"
            onClick={() => setOpen(false)}
            disabled={pending}
            className="button-secondary"
          >
            Cancel
          </button>
        )}
      </div>
    </form>
  );
}
