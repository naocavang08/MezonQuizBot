import apiClient from "./ApiClient";
import type {
    CreateQuizSessionDto,
    CreateSessionApiResponse,
    JoinQuizSessionDto,
    PagedQuizSessionDto,
    QuizSessionQueryParams,
    SessionApiResponse,
    SessionParticipantDto,
    QuizSessionDto,
} from "../Interface/session.dto";

const normalizePagedSessions = (
    raw: unknown,
    fallbackPage = 1,
    fallbackPageSize = 10
): PagedQuizSessionDto => {
    if (Array.isArray(raw)) {
        const items = raw as QuizSessionDto[];
        return {
            items,
            totalCount: items.length,
            page: fallbackPage,
            pageSize: fallbackPageSize,
            totalPages: items.length > 0 ? 1 : 0,
        };
    }

    const data = (raw ?? {}) as Record<string, unknown>;
    const items = (Array.isArray(data.items) ? data.items : data.Items) as QuizSessionDto[] | undefined;
    const totalCount = Number(data.totalCount ?? data.TotalCount ?? items?.length ?? 0);
    const page = Number(data.page ?? data.Page ?? fallbackPage);
    const pageSize = Number(data.pageSize ?? data.PageSize ?? fallbackPageSize);
    const totalPages = Number(data.totalPages ?? data.TotalPages ?? (totalCount > 0 ? Math.ceil(totalCount / Math.max(pageSize, 1)) : 0));

    return {
        items: Array.isArray(items) ? items : [],
        totalCount,
        page,
        pageSize,
        totalPages,
    };
};

export const createQuizSession = (body: CreateQuizSessionDto) => {
    return apiClient.post<CreateSessionApiResponse>("/api/QuizSession", body).then((res) => res.data);
};

export const joinQuizSession = (sessionId: string, body: JoinQuizSessionDto) => {
    return apiClient.post<SessionApiResponse>(`/api/QuizSession/${sessionId}/join`, body).then((res) => res.data);
};

export const getSessionDetails = (sessionId: string) => {
    return apiClient.get<QuizSessionDto>(`/api/QuizSession/${sessionId}`).then((res) => res.data);
};

export const getQuizSessions = (params?: QuizSessionQueryParams) => {
    return apiClient
        .get("/api/QuizSession", { params })
        .then((res) => normalizePagedSessions(res.data, params?.page ?? 1, params?.pageSize ?? 10));
};

export const startQuizSession = (sessionId: string) => {
    return apiClient.post<SessionApiResponse>(`/api/QuizSession/${sessionId}/start`).then((res) => res.data);
};

export const pauseQuizSession = (sessionId: string) => {
    return apiClient.post<SessionApiResponse>(`/api/QuizSession/${sessionId}/pause`).then((res) => res.data);
};

export const resumeQuizSession = (sessionId: string) => {
    return apiClient.post<SessionApiResponse>(`/api/QuizSession/${sessionId}/resume`).then((res) => res.data);
};

export const finishQuizSession = (sessionId: string) => {
    return apiClient.post<SessionApiResponse>(`/api/QuizSession/${sessionId}/finish`).then((res) => res.data);
};

export const deleteQuizSession = (sessionId: string) => {
    return apiClient.delete<SessionApiResponse>(`/api/QuizSession/${sessionId}`).then((res) => res.data);
};

export const getSessionLeaderboard = (sessionId: string) => {
    return apiClient.get<SessionParticipantDto[]>(`/api/QuizSession/${sessionId}/leaderboard`).then((res) => res.data);
};
