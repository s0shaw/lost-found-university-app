export type QuestionOption = { id: string; text: string };
export type Question = { id: string; text: string; options: QuestionOption[] };
export type Category = { id: string; name: string; questions: Question[] };
export type Location = { id: string; name: string; isHandoverPoint: boolean };
