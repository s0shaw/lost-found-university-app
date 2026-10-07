"use client";

import { useState, type FormEvent } from "react";
import { Field } from "@/components/field";
import { trackReport } from "@/features/tracking/api";
import { TrackedReport } from "@/features/tracking/tracked-report";
import type { TrackedReport as TrackedReportData, TrackRequest } from "@/features/tracking/types";
import { ApiError } from "@/lib/api";

// The lookup is a POST, so this page cannot be a server component driven by the URL: the pair
// must never sit in a link.
export default function TrackPage() {
  const [request, setRequest] = useState<TrackRequest | null>(null);
  const [report, setReport] = useState<TrackedReportData | null>(null);
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);

  async function lookup(next: TrackRequest) {
    setPending(true);
    setErrors({});
    try {
      setReport(await trackReport(next));
      setRequest(next);
    } catch (error) {
      setReport(null);
      // An unknown university id, a wrong code and someone else's pair all answer 404 on purpose.
      // Telling them apart here would reopen the leak the API closed.
      setErrors(
        error instanceof ApiError && error.status !== 404
          ? (error.problem.errors ?? { form: [error.message] })
          : { form: ["No report matches that university ID and tracking code."] },
      );
    } finally {
      setPending(false);
    }
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    await lookup({
      universityId: String(data.get("universityId") ?? ""),
      trackingCode: String(data.get("trackingCode") ?? ""),
    });
  }

  if (report && request) {
    return (
      <TrackedReport
        report={report}
        request={request}
        // Re-reads the report instead of patching the status locally: the cancellation is the
        // API's to describe.
        onCancelled={() => lookup(request)}
      />
    );
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-8">
      <div className="flex flex-col gap-3">
        <h1 className="text-3xl font-bold">Track a report</h1>
        <p className="text-ink-muted">
          Your university ID and the tracking code you were given open your own report, including
          the details nobody else can see.
        </p>
      </div>

      {errors.form && (
        <p role="alert" className="text-error">
          {errors.form.join(" ")}
        </p>
      )}

      <Field
        id="universityId"
        label="University ID"
        hint="The ID on your student or staff card."
        errors={errors.UniversityId}
      >
        {(props) => <input {...props} name="universityId" required className="field-input" />}
      </Field>

      <Field
        id="trackingCode"
        label="Tracking code"
        hint="The code shown when you created the report."
        errors={errors.TrackingCode}
      >
        {(props) => <input {...props} name="trackingCode" required className="field-input" />}
      </Field>

      <div>
        <button type="submit" disabled={pending} className="button">
          {pending ? "Looking…" : "Show my report"}
        </button>
      </div>
    </form>
  );
}
