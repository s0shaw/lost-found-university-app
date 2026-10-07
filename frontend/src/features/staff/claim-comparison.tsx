import Link from "next/link";
import { formatDate } from "@/features/reports/report-list";
import { StaffActionButton } from "./staff-action-button";
import type { StaffClaimComparison } from "./types";

/** Where the decision is actually made: the two secret descriptions side by side, then the
    answers. Every judgement shown here — whether an answer matches, how much text overlaps,
    whether the dates conflict — comes from the API; this view does no scoring of its own. */
export function ClaimComparison({ comparison }: { comparison: StaffClaimComparison }) {
  const { claim } = comparison;

  return (
    <article className="flex flex-col gap-8">
      <div className="flex flex-col gap-2">
        <p className="text-sm text-ink-muted">
          {claim.trackingCode} · {claim.status}
          {claim.source === "StaffSuggested" && " · suggested by staff"}
        </p>
        <h1 className="text-3xl font-bold">{claim.reportTitle}</h1>
        <p className="text-ink-muted">
          Claimed by {claim.claimantFullName} ({claim.claimantUniversityId}) ·{" "}
          <Link href={`/staff/reports/${claim.reportId}`} className="text-accent underline">
            Open the report
          </Link>
        </p>
      </div>

      <ClaimDetails comparison={comparison} />
    </article>
  );
}

/** Everything below the claim's heading: the numbers, the two descriptions, the answers and the
    decision. The report page embeds this per claim, so a decision never needs a second page. */
export function ClaimDetails({ comparison }: { comparison: StaffClaimComparison }) {
  const { claim } = comparison;
  const pending = claim.status === "Pending";
  const shared = comparison.sharedWords;

  return (
    <div className="flex flex-col gap-8">
      <dl className="grid gap-3 border-y border-line py-6 sm:grid-cols-[12rem_1fr]">
        <dt className="font-bold">Score</dt>
        <dd>{comparison.score}</dd>
        <dt className="font-bold">Answers matched</dt>
        <dd>
          {comparison.matchedAnswers} of {comparison.totalAnswers}
        </dd>
        <dt className="font-bold">Text overlap</dt>
        <dd>{Math.round(comparison.textOverlap * 100)}%</dd>
        <dt className="font-bold">Shared words</dt>
        <dd>{comparison.sharedWords.length > 0 ? comparison.sharedWords.join(", ") : "none"}</dd>
        <dt className="font-bold">Same location</dt>
        {/* Null means the claim carries no location to compare, which is not the same as "no". */}
        <dd>
          {comparison.sameLocation === null ? "not comparable" : yesNo(comparison.sameLocation)}
        </dd>
        <dt className="font-bold">Timeline</dt>
        <dd>
          {comparison.timelineConflict
            ? "conflict — the claimant lost it after the item was found"
            : "consistent"}
        </dd>
        {claim.lostOn && (
          <>
            <dt className="font-bold">Claimant lost it on</dt>
            <dd>{formatDate(claim.lostOn)}</dd>
          </>
        )}
        {comparison.handoverPointName && (
          <>
            <dt className="font-bold">Handover point</dt>
            <dd>{comparison.handoverPointName}</dd>
          </>
        )}
      </dl>

      <section className="flex flex-col gap-4">
        <h2 className="text-xl font-bold">Identifying details</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-2">
            <h3 className="font-bold">On the report</h3>
            <SharedWordsMarked text={comparison.reportSecretDescription} words={shared} />
          </div>
          <div className="flex flex-col gap-2">
            <h3 className="font-bold">On the claim</h3>
            <SharedWordsMarked text={comparison.claimSecretDescription} words={shared} />
          </div>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-xl font-bold">Answers</h2>
        <table className="w-full border-collapse text-left">
          <thead>
            <tr className="border-b border-line">
              <th scope="col" className="py-2 pr-4">
                Question
              </th>
              <th scope="col" className="py-2 pr-4">
                On the report
              </th>
              <th scope="col" className="py-2 pr-4">
                On the claim
              </th>
              <th scope="col" className="py-2">
                Verdict
              </th>
            </tr>
          </thead>
          <tbody>
            {comparison.answers.map((answer) => (
              <tr key={answer.questionId} className="border-b border-line align-top">
                <td className="py-2 pr-4">{answer.question ?? "—"}</td>
                <td className="py-2 pr-4">{answer.reportAnswer ?? "—"}</td>
                <td className="py-2 pr-4">{answer.claimAnswer ?? "—"}</td>
                {/* In words, never by colour alone. */}
                <td className="py-2">{answer.matches ? "match" : "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {pending ? (
        <section className="flex flex-col gap-4 border-t border-line pt-6">
          <h2 className="text-xl font-bold">Decision</h2>
          <div className="flex flex-wrap items-start gap-3">
            <StaffActionButton
              endpoint={`/api/staff/claims/${claim.id}/approve`}
              label="Approve claim"
              confirm="Approving matches the report to this claimant and rejects the other claims."
            />
            <StaffActionButton
              endpoint={`/api/staff/claims/${claim.id}/reject`}
              label="Reject claim"
              field="staffNote"
              secondary
            />
          </div>
        </section>
      ) : (
        <p className="border-t border-line pt-6 text-ink-muted">
          This claim was already {claim.status.toLowerCase()}
          {claim.decidedAt && ` on ${formatDate(claim.decidedAt.slice(0, 10))}`}.
        </p>
      )}
    </div>
  );
}

const yesNo = (value: boolean) => (value ? "yes" : "no");

/** The shared words the API sends are folded: lower case, Turkish letters mapped to ascii,
    everything else dropped. A word in the description has to be folded the same way before it
    can be recognised as one of them. */
const FOLDED: Record<string, string> = {
  ı: "i",
  İ: "i",
  ş: "s",
  ğ: "g",
  ç: "c",
  ö: "o",
  ü: "u",
};

const fold = (word: string) =>
  [...word]
    .map((character) => FOLDED[character] ?? character.toLowerCase())
    .join("")
    .replace(/[^a-z0-9]/g, "");

/** Same rule the scorer used to call the words shared: Turkish is agglutinative, so "cüzdanım"
    and "cüzdan" are the same word once four characters line up. */
const PREFIX_MATCH_LENGTH = 4;

function sameWord(left: string, right: string) {
  let shared = 0;

  while (shared < left.length && shared < right.length && left[shared] === right[shared]) {
    shared++;
  }

  return (shared === left.length && shared === right.length) || shared >= PREFIX_MATCH_LENGTH;
}

/** Marks the words both descriptions carry, so the eye finds them without reading the list
    under "Shared words" and mapping it back by hand. */
function SharedWordsMarked({ text, words }: { text: string; words: string[] }) {
  if (words.length === 0) {
    return <p>{text}</p>;
  }

  return (
    <p>
      {text.split(/(\s+)/).map((token, index) => {
        const folded = fold(token);

        return folded.length > 0 && words.some((word) => sameWord(folded, word)) ? (
          <mark key={index}>{token}</mark>
        ) : (
          token
        );
      })}
    </p>
  );
}
