export const QuestionType = {
    SingleChoice: 0,
    MultipleChoice: 1,
    TrueFalse: 2,
} as const;

export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

export const QuizVisibility = {
    Private: 0,
    Public: 1,
    Unlisted: 2,
} as const;

export type QuizVisibility = (typeof QuizVisibility)[keyof typeof QuizVisibility];

export const QuizStatus = {
    Draft: 0,
    Published: 1,
    Archived: 2,
} as const;

export type QuizStatus = (typeof QuizStatus)[keyof typeof QuizStatus];

export interface QuizOptionDto {
    id?: number;
    index: number;
    content: string;
    isCorrect: boolean;
}

export interface QuizQuestionDto {
    id?: number;
    index: number;
    content: string;
    mediaUrl?: string;
    timeLimitSeconds: number;
    points: number;
    questionType: QuestionType;
    options: QuizOptionDto[];
}

export interface QuizSettingsDto {
    shuffleQuestions: boolean;
    shuffleOptions: boolean;
    showCorrectAnswer: boolean;
    maxAttempts: number;
}

export interface QuizDto {
    id?: string;
    creatorId?: string;
    title: string;
    description?: string;
    categoryId?: string;
    questions: QuizQuestionDto[];
    totalPoints?: number;
    settings: QuizSettingsDto;
    visibility: QuizVisibility;
    status: QuizStatus;
    createdAt?: string;
    updatedAt?: string;
}

export interface ListQuizDto {
    id: string;
    title: string;
}

export interface CreateQuizResponse {
    message: string;
}

export interface UpdateQuizResponse {
    message: string;
}

export interface DeleteQuizResponse {
    message: string;
}

export interface QuestionOperationResponse {
    message: string;
}

export interface OptionOperationResponse {
    message: string;
}

export type OperationResponse = {
    message: string;
};