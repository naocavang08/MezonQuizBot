export type DashboardKpiDto = {
	users: number;
	quizzes: number;
	categories: number;
	sessions: number;
	participants: number;
	answers: number;
};

export type DashboardStatusCountDto = {
	label: string;
	count: number;
};

export type DashboardCategoryStatDto = {
	categoryName: string;
	quizCount: number;
};

export type DashboardDailyStatDto = {
	date: string;
	users: number;
	quizzes: number;
	sessions: number;
};

export type DashboardSummaryDto = {
	kpis: DashboardKpiDto;
	quizStatusDistribution: DashboardStatusCountDto[];
	sessionStatusDistribution: DashboardStatusCountDto[];
	topCategories: DashboardCategoryStatDto[];
	dailyStats: DashboardDailyStatDto[];
	generatedAt: string;
};

export type TopUserAnalyticsQuery = {
	dateFrom?: string;
	dateTo?: string;
	search?: string;
	minSessions?: number;
	sortBy?: string;
	sortDirection?: "asc" | "desc";
	page?: number;
	pageSize?: number;
};

export type TopUserAnalyticsSummaryDto = {
	totalActiveUsers: number;
	totalParticipations: number;
	totalSessions: number;
	averageAccuracy: number;
	averageScorePerUser: number;
};

export type TopUserAnalyticsItemDto = {
	rank: number;
	userId: string;
	displayName: string;
	avatarUrl?: string;
	totalScore: number;
	totalCorrectAnswers: number;
	totalAnswers: number;
	accuracyRate: number;
	totalSessions: number;
	firstSeenAt: string;
	lastSeenAt: string;
};

export type TopUserAnalyticsPaginationDto = {
	page: number;
	pageSize: number;
	totalCount: number;
	totalPages: number;
};

export type TopUserAnalyticsResponseDto = {
	summary: TopUserAnalyticsSummaryDto;
	items: TopUserAnalyticsItemDto[];
	pagination: TopUserAnalyticsPaginationDto;
	generatedAt: string;
};
