export type SessionStatus = 0 | 1 | 2 | 3 | 4;

export const SessionStatusValue = {
    Waiting: 0,
    Active: 1,
    Paused: 2,
    Finished: 3,
    Cancelled: 4,
} as const;

export interface CreateQuizSessionDto {
    quizId: string;
    maxParticipants?: number;
    deepLink?: string;
    qrCodeUrl?: string;
    mezonChannelId?: string;
}

export interface QuizSessionDto {
    id: string;
    quizId: string;
    quizTitle: string;
    hostId: string;
    status: SessionStatus;
    currentQuestion: number;
    deepLink?: string;
    qrCodeUrl?: string;
    mezonChannelId?: string;
    maxParticipants?: number;
    participantCount: number;
    startedAt?: string;
    finishedAt?: string;
    createdAt: string;
}

export interface QuizSessionQueryParams {
    hostId?: string;
    quizId?: string;
    status?: SessionStatus;
    page?: number;
    pageSize?: number;
}

export interface PagedQuizSessionDto {
    items: QuizSessionDto[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export interface SessionApiResponse {
    message: string;
}

export interface CreateSessionApiResponse extends SessionApiResponse {
    session: QuizSessionDto;
}

export interface JoinQuizSessionDto {
    userId: string;
}

export interface ClearParticipantDto {
    userId: string;
}

export interface SubmitAnswerDto {
    userId: string;
    selectedOption: number;
    responseTimeMs?: number;
}

export interface QuizSessionQuestionOptionDto {
    index: number;
    content: string;
}

export interface QuizSessionQuestionDto {
    sessionId: string;
    questionIndex: number;
    content: string;
    mediaUrl?: string;
    timeLimitSeconds: number;
    points: number;
    options: QuizSessionQuestionOptionDto[];
}

export interface SessionStateChangedDto {
    sessionId: string;
    status: SessionStatus;
    currentQuestion: number;
    sentAt: string;
}

export interface SessionParticipantDto {
    userId: string;
    displayName: string;
    totalScore: number;
    answersCount: number;
    correctCount: number;
    rank?: number;
    joinedAt: string;
}
