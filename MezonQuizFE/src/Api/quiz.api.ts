import apiClient from "./ApiClient";
import type {
    CreateQuizResponse,
    DeleteQuizResponse,
    ListQuizDto,
    OptionOperationResponse,
    QuestionOperationResponse,
    QuizDto,
    QuizOptionDto,
    QuizQuestionDto,
    UpdateQuizResponse,
} from "../Interface/quiz.dto";

export const getQuizzes = (userId?: string) => {
    return apiClient
        .get<ListQuizDto[]>("/api/Quiz", {
            params: userId ? { userId } : undefined,
        })
        .then((res) => {
            return res.data;
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