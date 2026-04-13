import apiClient from "./ApiClient";
import type {
	DashboardAuditLogDto,
	DashboardCategoryStatDto,
	DashboardDailyStatDto,
	DashboardKpiDto,
	DashboardStatusCountDto,
	DashboardSummaryDto,
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
		recentActivities: (Array.isArray(data.recentActivities ?? data.RecentActivities)
			? (data.recentActivities ?? data.RecentActivities)
			: []) as DashboardAuditLogDto[],
		generatedAt: toString(data.generatedAt ?? data.GeneratedAt),
	};
};

export const getDashboardSummary = (params?: { days?: number; recentLimit?: number }) => {
	return apiClient
		.get("/api/Dashboard/summary", { params })
		.then((res) => normalizeDashboardSummary(res.data));
};
