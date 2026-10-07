import type { ReactNode } from "react";

type FieldProps = {
  id: string;
  label: string;
  /** Field errors from ProblemDetails. The backend sends field names in PascalCase. */
  errors?: string[];
  hint?: string;
  children: (props: {
    id: string;
    "aria-invalid": boolean | undefined;
    "aria-describedby": string | undefined;
  }) => ReactNode;
};

/** The one shared form building block: label, hint and error are wired to the control once,
    so no form has to repeat the accessibility plumbing by hand. */
export function Field({ id, label, errors, hint, children }: FieldProps) {
  const hasError = (errors?.length ?? 0) > 0;
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = hasError ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className="flex flex-col gap-2">
      <label htmlFor={id} className="font-bold">
        {label}
      </label>
      {hint && (
        <p id={hintId} className="text-sm text-ink-muted">
          {hint}
        </p>
      )}
      {hasError && (
        <p id={errorId} role="alert" className="text-sm text-error">
          {errors!.join(" ")}
        </p>
      )}
      {children({
        id,
        "aria-invalid": hasError || undefined,
        "aria-describedby": describedBy,
      })}
    </div>
  );
}
