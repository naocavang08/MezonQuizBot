import apiClient from "./ApiClient";
import type {
    CreateQuizResponse,
    DeleteQuizResponse,
    ListQuizDto,
    OptionOperationResponse,
    PagedQuizListDto,
    QuizListQueryParams,
    QuestionOperationResponse,
    QuizDto,
    QuizOptionDto,
    QuizQuestionDto,
    UpdateQuizResponse,
} from "../Interface/quiz.dto";

const normalizePagedQuizList = (raw: unknown, fallbackPage = 1, fallbackPageSize = 10): PagedQuizListDto<ListQuizDto> => {
    if (Array.isArray(raw)) {
        const items = raw as ListQuizDto[];
        return {
            items,
            totalCount: items.length,
            page: fallbackPage,
            pageSize: fallbackPageSize,
            totalPages: items.length > 0 ? 1 : 0,
        };
    }

    const data = (raw ?? {}) as Record<string, unknown>;
    const items = (Array.isArray(data.items) ? data.items : data.Items) as ListQuizDto[] | undefined;
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

export const getQuizzes = (params?: QuizListQueryParams) => {
    return apiClient
        .get("/api/Quiz", {
            params,
        })
        .then((res) => {
            return normalizePagedQuizList(
                res.data,
                params?.page ?? 1,
                params?.pageSize ?? 10
            );
        });
};

export const getQuizDetails = (id: string) => {
    return apiClient
        .get<QuizDto>(`/api/Quiz/${id}`)
        .then((res) => {
            return res.data;
        });
};

export const createQuiz = (body: QuizDto, userId?: string) => {
    return apiClient
        .post<CreateQuizResponse>("/api/Quiz", body, {
            params: userId ? { userId } : undefined,
        })
        .then((res) => {
            return res.data;
        });
};

export const updateQuiz = (quizId: string, body: QuizDto) => {
    return apiClient
        .put<UpdateQuizResponse>(`/api/Quiz/${quizId}`, body)
        .then((res) => {
            return res.data;
        });
};

export const deleteQuiz = (quizId: string) => {
    return apiClient
        .delete<DeleteQuizResponse>(`/api/Quiz/${quizId}`)
        .then((res) => {
            return res.data;
        });
};

export const addQuestion = (quizId: string, question: QuizQuestionDto) => {
    return apiClient
        .post<QuestionOperationResponse>(`/api/Quiz/${quizId}/questions`, question)
        .then((res) => {
            return res.data;
        });
};

export const updateQuestion = (quizId: string, questionIndex: number, question: QuizQuestionDto) => {
    return apiClient
        .put<QuestionOperationResponse>(
            `/api/Quiz/${quizId}/questions/${questionIndex}`,
            question
        )
        .then((res) => {
            return res.data;
        });
};

export const deleteQuestion = (quizId: string, questionIndex: number) => {
    return apiClient
        .delete<QuestionOperationResponse>(
            `/api/Quiz/${quizId}/questions/${questionIndex}`
        )
        .then((res) => {
            return res.data;
        });
};

export const addOption = (quizId: string, questionIndex: number, option: QuizOptionDto) => {
    return apiClient
        .post<OptionOperationResponse>(
            `/api/Quiz/${quizId}/questions/${questionIndex}/options`,
            option
        )
        .then((res) => {
            return res.data;
        });
};

export const updateOption = (
    quizId: string,
    questionIndex: number,
    optionIndex: number,
    option: QuizOptionDto
) => {
    return apiClient
        .put<OptionOperationResponse>(
            `/api/Quiz/${quizId}/questions/${questionIndex}/options/${optionIndex}`,
            option
        )
        .then((res) => {
            return res.data;
        });
};

export const deleteOption = (
    quizId: string,
    questionIndex: number,
    optionIndex: number
) => {
    return apiClient
        .delete<OptionOperationResponse>(
            `/api/Quiz/${quizId}/questions/${questionIndex}/options/${optionIndex}`
        )
        .then((res) => {
            return res.data;
        });
};

export const updateQuizSettings = (quizId: string, settings: QuizDto["settings"]) => {
    return apiClient
        .put<UpdateQuizResponse>(`/api/Quiz/${quizId}/settings`, settings)
        .then((res) => {
            return res.data;
        });
};