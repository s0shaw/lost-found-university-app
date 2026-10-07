"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { Field } from "@/components/field";
import type { Category, Location } from "@/features/catalog/types";
import { SecretQuestions, collectAnswers } from "@/features/reports/secret-questions";
import type { ItemReportDetail } from "@/features/reports/types";
import { ApiError } from "@/lib/api";
import { createClaim } from "./api";
import type { CreatedClaim } from "./types";

export function ClaimForm({
  report,
  category,
  handoverPoints,
}: {
  report: ItemReportDetail;
  category: Category;
  handoverPoints: Location[];
}) {
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);
  const [created, setCreated] = useState<CreatedClaim | null>(null);

  // Claiming a lost report means the claimant is holding the item and has to drop it off.
  // Claiming a found report means it is already on its way to a collection point.
  const holdingTheItem = report.type === "Lost";

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setPending(true);
    setErrors({});
    try {
      setCreated(
        await createClaim({
          reportId: report.id,
          universityId: String(data.get("universityId") ?? ""),
          secretDescription: String(data.get("secretDescription") ?? ""),
          answers: collectAnswers(category, data),
          lostOn: holdingTheItem ? undefined : String(data.get("lostOn") ?? "") || undefined,
          handoverPointId: holdingTheItem ? String(data.get("handoverPointId") ?? "") : undefined,
        }),
      );
    } catch (error) {
      setErrors(
        error instanceof ApiError
          ? (error.problem.errors ?? { form: [error.message] })
          : { form: ["Something went wrong. Try again."] },
      );
    } finally {
      setPending(false);
    }
  }

  if (created) {
    return (
      <div className="flex flex-col gap-6">
        <h1 className="text-3xl font-bold">Claim sent</h1>
        <div className="border border-ink p-4">
          <p className="text-sm text-ink-muted">Your tracking code</p>
          <p className="text-3xl font-bold">{created.trackingCode}</p>
        </div>
        <p>
          Write this down. Staff compare your answers with the ones on the report and decide. Your
          claim is {created.status.toLowerCase()} until then.
        </p>
        <div>
          <Link href={`/reports/${report.id}`} className="button">
            Back to the report
          </Link>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-8">
      <div className="flex flex-col gap-3">
        <h1 className="text-3xl font-bold">
          {holdingTheItem ? "I found this item" : "This item is mine"}
        </h1>
        <p className="text-ink-muted">
          {report.title} · {report.categoryName}
        </p>
        <p className="text-ink-muted">
          {holdingTheItem
            ? "Answer the questions from the report, then say where you will hand the item in."
            : "Answer the questions from the report. Staff compare them with the owner's answers."}
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

      {!holdingTheItem && (
        <Field
          id="lostOn"
          label="When did you lose it?"
          hint="Optional. A date close to the report makes the match stronger."
          errors={errors.LostOn}
        >
          {(props) => <input {...props} name="lostOn" type="date" className="field-input" />}
        </Field>
      )}

      {holdingTheItem && (
        <Field
          id="handoverPointId"
          label="Where will you hand it in?"
          errors={errors.HandoverPointId}
        >
          {(props) => (
            <select {...props} name="handoverPointId" required className="field-input">
              <option value="">Choose a collection point</option>
              {handoverPoints.map((location) => (
                <option key={location.id} value={location.id}>
                  {location.name}
                </option>
              ))}
            </select>
          )}
        </Field>
      )}

      <Field
        id="secretDescription"
        label="Identifying details"
        hint="Only staff see this. Describe the item as closely as you can."
        errors={errors.SecretDescription}
      >
        {(props) => (
          <textarea {...props} name="secretDescription" rows={4} required className="field-input" />
        )}
      </Field>

      <SecretQuestions category={category} errors={errors.Answers} />

      <div>
        <button type="submit" disabled={pending} className="button">
          {pending ? "Sending…" : "Send claim"}
        </button>
      </div>
    </form>
  );
}
