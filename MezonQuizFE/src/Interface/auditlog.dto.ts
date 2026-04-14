export type AuditLogDetailsDto = {
	title?: string;
	description?: string;
	status?: string;
	participantCount?: number | null;
};

export type AuditLogItemDto = {
	id: string;
	userDisplayName?: string;
	action: string;
	resourceType?: string | null;
	details?: AuditLogDetailsDto | null;
	ipAddress?: string | null;
	createdAt: string;
};

export type AuditLogQueryParams = {
	page?: number;
	pageSize?: number;
	action?: string;
	resourceType?: string;
	user?: string;
	status?: string;
	fromDate?: string;
	toDate?: string;
};

export type PagedAuditLogDto = {
	items: AuditLogItemDto[];
	totalCount: number;
	page: number;
	pageSize: number;
	totalPages: number;
};
