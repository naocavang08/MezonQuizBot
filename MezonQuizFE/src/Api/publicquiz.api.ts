import type { ListPublicQuizDto, PagedQuizListDto, QuizListQueryParams } from "../Interface/quiz.dto";
import apiClient from "./ApiClient";

const normalizePagedQuizList = (raw: unknown, fallbackPage = 1, fallbackPageSize = 10): PagedQuizListDto<ListPublicQuizDto> => {
    if (Array.isArray(raw)) {
        const items = raw as ListPublicQuizDto[];
        return {
            items,
            totalCount: items.length,
            page: fallbackPage,
            pageSize: fallbackPageSize,
            totalPages: items.length > 0 ? 1 : 0,
        };
    }

    const data = (raw ?? {}) as Record<string, unknown>;
    const items = (Array.isArray(data.items) ? data.items : data.Items) as ListPublicQuizDto[] | undefined;
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

export const getPublicQuizzes = (params?: QuizListQueryParams) => {
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