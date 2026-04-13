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

export type DashboardAuditDetailsDto = {
	title?: string;
	description?: string;
	status?: string;
	participantCount?: number | null;
};

export type DashboardAuditLogDto = {
	id: string;
	userDisplayName: string;
	action: string;
	resourceType?: string | null;
	details?: DashboardAuditDetailsDto | null;
	ipAddress?: string | null;
	createdAt: string;
};

export type DashboardSummaryDto = {
	kpis: DashboardKpiDto;
	quizStatusDistribution: DashboardStatusCountDto[];
	sessionStatusDistribution: DashboardStatusCountDto[];
	topCategories: DashboardCategoryStatDto[];
	dailyStats: DashboardDailyStatDto[];
	recentActivities: DashboardAuditLogDto[];
	generatedAt: string;
};
