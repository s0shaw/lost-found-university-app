import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { Category } from "@/features/catalog/types";
import { SecretQuestions, collectAnswers } from "./secret-questions";

const wallets: Category = {
  id: "c1",
  name: "Wallets & Cards",
  questions: [
    {
      id: "q1",
      text: "What colour is it?",
      options: [
        { id: "o1", text: "Black" },
        { id: "o2", text: "Brown" },
      ],
    },
  ],
};

const bags: Category = {
  id: "c2",
  name: "Bags",
  questions: [{ id: "q2", text: "How many pockets?", options: [{ id: "o3", text: "One" }] }],
};

describe("SecretQuestions", () => {
  it("asks the visitor to pick a category before showing questions", () => {
    render(<SecretQuestions category={undefined} />);
    expect(screen.getByText(/choose a category/i)).toBeInTheDocument();
  });

  it("renders only the questions of the chosen category", () => {
    const { rerender } = render(<SecretQuestions category={wallets} />);
    expect(screen.getByLabelText("What colour is it?")).toBeInTheDocument();

    rerender(<SecretQuestions category={bags} />);
    expect(screen.queryByLabelText("What colour is it?")).not.toBeInTheDocument();
    expect(screen.getByLabelText("How many pockets?")).toBeInTheDocument();
  });

  it("shows the answer set error once rather than under every question", () => {
    render(<SecretQuestions category={wallets} errors={["Answer every question."]} />);
    expect(screen.getAllByRole("alert")).toHaveLength(1);
  });
});

describe("collectAnswers", () => {
  it("pairs each question with the option chosen for it", () => {
    const data = new FormData();
    data.set("answer-q1", "o2");
    expect(collectAnswers(wallets, data)).toEqual([{ questionId: "q1", optionId: "o2" }]);
  });
});
