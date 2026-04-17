import apiClient from "./ApiClient";
import type {
	DashboardCategoryStatDto,
	DashboardDailyStatDto,
	DashboardKpiDto,
	DashboardStatusCountDto,
	DashboardSummaryDto,
	TopUserAnalyticsItemDto,
	TopUserAnalyticsPaginationDto,
	TopUserAnalyticsQuery,
	TopUserAnalyticsResponseDto,
	TopUserAnalyticsSummaryDto,
} from "../Interface/dashboard.dto";

type AnyRecord = Record<string, unknown>;

const toNumber = (value: unknown) => {
	const parsed = Number(value);
	return Number.isFinite(parsed) ? parsed : 0;
};

const toString = (value: unknown) => (typeof value === "string" ? value : "");

const toArray = <T>(value: unknown, mapItem: (item: unknown) => T): T[] => {
	if (!Array.isArray(value)) {
		return [];
	}

	return value.map(mapItem);
};

const normalizeKpis = (raw: unknown): DashboardKpiDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		users: toNumber(data.users ?? data.Users),
		quizzes: toNumber(data.quizzes ?? data.Quizzes),
		categories: toNumber(data.categories ?? data.Categories),
		sessions: toNumber(data.sessions ?? data.Sessions),
		participants: toNumber(data.participants ?? data.Participants),
		answers: toNumber(data.answers ?? data.Answers),
	};
};

const normalizeStatusCount = (raw: unknown): DashboardStatusCountDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		label: toString(data.label ?? data.Label),
		count: toNumber(data.count ?? data.Count),
	};
};

const normalizeTopCategory = (raw: unknown): DashboardCategoryStatDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		categoryName: toString(data.categoryName ?? data.CategoryName),
		quizCount: toNumber(data.quizCount ?? data.QuizCount),
	};
};

const normalizeDailyStat = (raw: unknown): DashboardDailyStatDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		date: toString(data.date ?? data.Date),
		users: toNumber(data.users ?? data.Users),
		quizzes: toNumber(data.quizzes ?? data.Quizzes),
		sessions: toNumber(data.sessions ?? data.Sessions),
	};
};

const normalizeDashboardSummary = (raw: unknown): DashboardSummaryDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		kpis: normalizeKpis(data.kpis ?? data.Kpis),
		quizStatusDistribution: toArray(
			data.quizStatusDistribution ?? data.QuizStatusDistribution,
			normalizeStatusCount,
		),
		sessionStatusDistribution: toArray(
			data.sessionStatusDistribution ?? data.SessionStatusDistribution,
			normalizeStatusCount,
		),
		topCategories: toArray(data.topCategories ?? data.TopCategories, normalizeTopCategory),
		dailyStats: toArray(data.dailyStats ?? data.DailyStats, normalizeDailyStat),
		generatedAt: toString(data.generatedAt ?? data.GeneratedAt),
	};
};

const normalizeTopUserSummary = (raw: unknown): TopUserAnalyticsSummaryDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		totalActiveUsers: toNumber(data.totalActiveUsers ?? data.TotalActiveUsers),
		totalParticipations: toNumber(data.totalParticipations ?? data.TotalParticipations),
		totalSessions: toNumber(data.totalSessions ?? data.TotalSessions),
		averageAccuracy: toNumber(data.averageAccuracy ?? data.AverageAccuracy),
		averageScorePerUser: toNumber(data.averageScorePerUser ?? data.AverageScorePerUser),
	};
};

const normalizeTopUserItem = (raw: unknown): TopUserAnalyticsItemDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		rank: toNumber(data.rank ?? data.Rank),
		userId: toString(data.userId ?? data.UserId),
		displayName: toString(data.displayName ?? data.DisplayName),
		avatarUrl: toString(data.avatarUrl ?? data.AvatarUrl) || undefined,
		totalScore: toNumber(data.totalScore ?? data.TotalScore),
		totalCorrectAnswers: toNumber(data.totalCorrectAnswers ?? data.TotalCorrectAnswers),
		totalAnswers: toNumber(data.totalAnswers ?? data.TotalAnswers),
		accuracyRate: toNumber(data.accuracyRate ?? data.AccuracyRate),
		totalSessions: toNumber(data.totalSessions ?? data.TotalSessions),
		firstSeenAt: toString(data.firstSeenAt ?? data.FirstSeenAt),
		lastSeenAt: toString(data.lastSeenAt ?? data.LastSeenAt),
	};
};

const normalizeTopUserPagination = (raw: unknown): TopUserAnalyticsPaginationDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		page: toNumber(data.page ?? data.Page) || 1,
		pageSize: toNumber(data.pageSize ?? data.PageSize) || 20,
		totalCount: toNumber(data.totalCount ?? data.TotalCount),
		totalPages: toNumber(data.totalPages ?? data.TotalPages),
	};
};

const normalizeTopUserAnalytics = (raw: unknown): TopUserAnalyticsResponseDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		summary: normalizeTopUserSummary(data.summary ?? data.Summary),
		items: toArray(data.items ?? data.Items, normalizeTopUserItem),
		pagination: normalizeTopUserPagination(data.pagination ?? data.Pagination),
		generatedAt: toString(data.generatedAt ?? data.GeneratedAt),
	};
};

export const getDashboardSummary = (params?: { days?: number }) => {
	return apiClient
		.get("/api/Dashboard/summary", { params })
		.then((res) => normalizeDashboardSummary(res.data));
};

export const getTopUserAnalytics = (params: TopUserAnalyticsQuery) => {
	return apiClient
		.get("/api/Leaderboard", { params })
		.then((res) => normalizeTopUserAnalytics(res.data));
};
