import apiClient from "./ApiClient";
import type {
    CreateQuizResponse,
    DeleteQuizResponse,
    OptionOperationResponse,
    PagedQuizListDto,
    QuizListQueryParams,
    QuestionOperationResponse,
    QuizDto,
    QuizOptionDto,
    QuizQuestionDto,
    UpdateQuizResponse,
    SaveQuizDto,
    AvailableQuizDto,
    Quiz,
} from "../Interface/quiz.dto";

const normalizePagedQuizList = <T extends AvailableQuizDto | QuizDto>(raw: unknown, fallbackPage = 1, fallbackPageSize = 10): PagedQuizListDto<T> => {
    if (Array.isArray(raw)) {
        const items = raw as T[];
        return {
            items,
            totalCount: items.length,
            page: fallbackPage,
            pageSize: fallbackPageSize,
            totalPages: items.length > 0 ? 1 : 0,
        };
    }

    const data = (raw ?? {}) as Record<string, unknown>;
    const items = (Array.isArray(data.items) ? data.items : data.Items) as T[] | undefined;
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

export const getAvailableQuizzes = (params?: QuizListQueryParams) => {
    return apiClient
        .get("/api/Quiz/available-quiz", {
            params,
        })
        .then((res) => {
            return normalizePagedQuizList<AvailableQuizDto>(
                res.data,
                params?.page ?? 1,
                params?.pageSize ?? 10
            );
        });
};

export const getAvailableQuiz = (id: string) => {
    return apiClient
        .get<AvailableQuizDto>(`/api/Quiz/available-quiz/${id}`)
        .then((res) => {
            return res.data;
        }
    );
};

export const getAllQuizzes = (params?: QuizListQueryParams) => {
    return apiClient
        .get("/api/Quiz", {
            params,
        })
        .then((res) => {
            return normalizePagedQuizList<QuizDto>(
                res.data,
                params?.page ?? 1,
                params?.pageSize ?? 10
            );
        });
};

export const getQuiz = (id: string) => {
    return apiClient
        .get<Quiz>(`/api/Quiz/${id}`)
        .then((res) => {
            return res.data;
        });
};

export const createQuiz = (body: SaveQuizDto) => {
    return apiClient
        .post<CreateQuizResponse>("/api/Quiz", body)
        .then((res) => {
            return res.data;
        });
};

export const updateQuiz = (quizId: string, body: SaveQuizDto) => {
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

export const updateQuizSettings = (quizId: string, settings: Quiz["settings"]) => {
    return apiClient
        .put<UpdateQuizResponse>(`/api/Quiz/${quizId}/settings`, settings)
        .then((res) => {
            return res.data;
        });
};

export const uploadQuizMedia = (file: File) => {
    const formData = new FormData();
    formData.append("file", file);

    return apiClient
        .post<{ url?: string; Url?: string }>("/api/Quiz/upload-media", formData, {
            headers: { "Content-Type": "multipart/form-data" },
        })
        .then((res) => res.data?.url ?? res.data?.Url ?? "");
};
