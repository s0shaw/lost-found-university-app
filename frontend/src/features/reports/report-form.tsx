"use client";

import { useState, type FormEvent } from "react";
import { Field } from "@/components/field";
import type { Category, Location } from "@/features/catalog/types";
import { ApiError } from "@/lib/api";
import { createReport } from "./api";
import { SecretQuestions, collectAnswers } from "./secret-questions";
import { TrackingCode } from "./tracking-code";
import type { CreatedReport, ItemReportType } from "./types";

export function ReportForm({
  type,
  categories,
  locations,
}: {
  type: ItemReportType;
  categories: Category[];
  locations: Location[];
}) {
  const [categoryId, setCategoryId] = useState("");
  const [errors, setErrors] = useState<Record<string, string[]>>({});
  const [pending, setPending] = useState(false);
  const [created, setCreated] = useState<CreatedReport | null>(null);

  const category = categories.find((candidate) => candidate.id === categoryId);
  const lost = type === "Lost";

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setPending(true);
    setErrors({});
    try {
      setCreated(
        await createReport({
          type,
          universityId: String(data.get("universityId") ?? ""),
          categoryId,
          title: String(data.get("title") ?? ""),
          publicDescription: String(data.get("publicDescription") ?? ""),
          locationId: String(data.get("locationId") ?? ""),
          occurredOn: String(data.get("occurredOn") ?? ""),
          secretDescription: String(data.get("secretDescription") ?? ""),
          answers: category ? collectAnswers(category, data) : [],
          // A lost report has no handover point at all: the API rejects one outright.
          handoverPointId: lost ? undefined : String(data.get("handoverPointId") ?? ""),
        }),
      );
    } catch (error) {
      // Field messages live in `errors`; a domain rule that matches no field only has `detail`.
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
    return <TrackingCode created={created} />;
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-8">
      <div className="flex flex-col gap-3">
        <h1 className="text-3xl font-bold">
          {lost ? "Report a lost item" : "Report a found item"}
        </h1>
        <p className="text-ink-muted">
          {lost
            ? "Describe the item in public, then answer the questions only its owner could answer."
            : "Describe the item in public, then record the details only its owner could know."}
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

      <Field id="title" label="Title" errors={errors.Title}>
        {(props) => (
          <input
            {...props}
            name="title"
            required
            maxLength={120}
            placeholder="Black leather wallet"
            className="field-input"
          />
        )}
      </Field>

      <Field
        id="publicDescription"
        label="Public description"
        hint="Everyone can read this. Leave out anything that proves the item is yours."
        errors={errors.PublicDescription}
      >
        {(props) => (
          <textarea {...props} name="publicDescription" rows={4} className="field-input" />
        )}
      </Field>

      <Field id="categoryId" label="Category" errors={errors.CategoryId}>
        {(props) => (
          <select
            {...props}
            name="categoryId"
            required
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
            className="field-input"
          >
            <option value="">Choose a category</option>
            {categories.map((candidate) => (
              <option key={candidate.id} value={candidate.id}>
                {candidate.name}
              </option>
            ))}
          </select>
        )}
      </Field>

      <Field
        id="locationId"
        label={lost ? "Where did you lose it?" : "Where did you find it?"}
        errors={errors.LocationId}
      >
        {(props) => (
          <select {...props} name="locationId" required className="field-input">
            <option value="">Choose a location</option>
            {locations.map((location) => (
              <option key={location.id} value={location.id}>
                {location.name}
              </option>
            ))}
          </select>
        )}
      </Field>

      <Field
        id="occurredOn"
        label={lost ? "When did you lose it?" : "When did you find it?"}
        errors={errors.OccurredOn}
      >
        {(props) => (
          <input {...props} name="occurredOn" type="date" required className="field-input" />
        )}
      </Field>

      {!lost && (
        <Field
          id="handoverPointId"
          label="Where will you hand it in?"
          hint="Drop the item at this desk so staff can return it."
          errors={errors.HandoverPointId}
        >
          {(props) => (
            <select {...props} name="handoverPointId" required className="field-input">
              <option value="">Choose a collection point</option>
              {locations
                .filter((location) => location.isHandoverPoint)
                .map((location) => (
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
        hint="Only staff see this. Scratches, contents, engravings — whatever proves it is yours."
        errors={errors.SecretDescription}
      >
        {(props) => (
          <textarea {...props} name="secretDescription" rows={4} required className="field-input" />
        )}
      </Field>

      <SecretQuestions category={category} errors={errors.Answers} />

      <div>
        <button type="submit" disabled={pending} className="button">
          {pending ? "Sending…" : "Create report"}
        </button>
      </div>
    </form>
  );
}
