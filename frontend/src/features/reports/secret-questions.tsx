import { Field } from "@/components/field";
import type { Category } from "@/features/catalog/types";

/** The questions of the chosen category. They arrive with the category in a single
    `/api/categories` call, so changing the category costs no request. */
export function SecretQuestions({
  category,
  errors,
}: {
  category: Category | undefined;
  errors?: string[];
}) {
  if (!category) {
    return <p className="text-ink-muted">Choose a category to see its questions.</p>;
  }

  return (
    <fieldset className="flex flex-col gap-6 card p-4">
      <legend className="px-2 font-bold">Identifying questions</legend>
      <p className="text-sm text-ink-muted">
        Only staff see these answers. Answer every question — the closer they are to the other
        report, the stronger the match.
      </p>
      {/* The API rejects the answer set as a whole, so its message belongs to the set, not
          repeated under every question. */}
      {errors && errors.length > 0 && (
        <p role="alert" className="text-sm text-error">
          {errors.join(" ")}
        </p>
      )}
      {category.questions.map((question) => (
        <Field key={question.id} id={`answer-${question.id}`} label={question.text}>
          {(props) => (
            <select {...props} name={`answer-${question.id}`} required className="field-input">
              <option value="">Choose an answer</option>
              {question.options.map((option) => (
                <option key={option.id} value={option.id}>
                  {option.text}
                </option>
              ))}
            </select>
          )}
        </Field>
      ))}
    </fieldset>
  );
}

/** Reads one `answer-<questionId>` value per question. The backend is the authority on
    "every question exactly once"; `required` above only saves the visitor a round trip. */
export function collectAnswers(category: Category, data: FormData) {
  return category.questions.map((question) => ({
    questionId: question.id,
    optionId: String(data.get(`answer-${question.id}`) ?? ""),
  }));
}
