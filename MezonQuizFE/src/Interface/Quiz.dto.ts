export const QuestionType = {
    SingleChoice: 0,
    MultipleChoice: 1,
    TrueFalse: 2,
} as const;

export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType];

export const QuizVisibility = {
    Private: 0, // Quiz chỉ chủ sở hữu hoặc người được cấp quyền mới xem được.
    Public: 1, // Quiz ai cũng có thể tìm và xem được.
    Unlisted: 2, // Quiz không hiển thị trong danh sách công khai nhưng ai có link vẫn có thể xem được.
} as const;

export type QuizVisibility = (typeof QuizVisibility)[keyof typeof QuizVisibility];

export const QuizStatus = {
    Draft: 0, // Quiz đang ở trạng thái nháp. Chỉ chủ sở hữu mới có thể xem và chỉnh sửa.
    Published: 1, // Quiz đã được xuất bản. Ai cũng có thể xem được nếu quiz là Public hoặc Unlisted. Chỉ chủ sở hữu mới có thể chỉnh sửa.
    Archived: 2, // Quiz đã được lưu trữ. Chỉ chủ sở hữu mới có thể xem và chỉnh sửa. Không hiển thị trong danh sách công khai.
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

export interface AvailableQuizDto {
    id: string;
    creatorId: string;
    title: string;
    description?: string;
    categoryId?: string;
    totalPoints: number;
}

export interface QuizDto {
    id?: string;
    creatorId?: string;
    title: string;
    description?: string;
    categoryId?: string;
    totalPoints?: number;
    visibility: QuizVisibility;
    status: QuizStatus;
    createdAt?: string;
    updatedAt?: string;
}

export interface Quiz {
    id: string;
    creatorId: string;
    title: string;
    description?: string;
    categoryId?: string;
    questions: QuizQuestionDto[];
    totalPoints: number;
    settings: QuizSettingsDto;
    visibility: QuizVisibility;
    status: QuizStatus;
    createdAt: string;
    updatedAt: string;
}

export interface SaveQuizDto {
    id?: string;
    title: string;
    description?: string;
    categoryId?: string;
    questions: QuizQuestionDto[];
    settings: QuizSettingsDto;
    visibility: QuizVisibility;
    status: QuizStatus;
}

export interface QuizListQueryParams {
    userId?: string;
    onlyMine?: boolean;
    category?: string;
    title?: string;
    page?: number;
    pageSize?: number;
}

export interface PagedQuizListDto<T extends AvailableQuizDto | QuizDto> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
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