import apiClient from "./ApiClient";
import type { AuditLogItemDto, AuditLogQueryParams, PagedAuditLogDto } from "../Interface/auditlog.dto";

type AnyRecord = Record<string, unknown>;

const toString = (value: unknown) => (typeof value === "string" ? value : "");

const normalizeAuditLog = (raw: unknown): AuditLogItemDto => {
	const data = (raw ?? {}) as AnyRecord;
	return {
		id: toString(data.id ?? data.Id),
		action: toString(data.action ?? data.Action),
		userDisplayName: toString(data.userDisplayName ?? data.UserDisplayName),
		resourceType: toString(data.resourceType ?? data.ResourceType) || null,
		ipAddress: toString(data.ipAddress ?? data.IpAddress) || null,
		details: ((data.details ?? data.Details) as AuditLogItemDto["details"]) ?? null,
		createdAt: toString(data.createdAt ?? data.CreatedAt),
	};
};

export const getRecentAuditLogs = (limit = 10) => {
	return apiClient
		.get("/api/AuditLog/recent", { params: { limit } })
		.then((res) => (Array.isArray(res.data) ? res.data.map(normalizeAuditLog) : []));
};

const normalizePagedAuditLogs = (
	raw: unknown,
	fallbackPage = 1,
	fallbackPageSize = 20,
): PagedAuditLogDto => {
	if (Array.isArray(raw)) {
		const items = raw.map(normalizeAuditLog);
		return {
			items,
			totalCount: items.length,
			page: fallbackPage,
			pageSize: fallbackPageSize,
			totalPages: items.length > 0 ? 1 : 0,
		};
	}

	const data = (raw ?? {}) as AnyRecord;
	const rawItems = Array.isArray(data.items) ? data.items : Array.isArray(data.Items) ? data.Items : [];
	const items = rawItems.map(normalizeAuditLog);

	return {
		items,
		totalCount: Number(data.totalCount ?? data.TotalCount ?? items.length),
		page: Number(data.page ?? data.Page ?? fallbackPage),
		pageSize: Number(data.pageSize ?? data.PageSize ?? fallbackPageSize),
		totalPages: Number(data.totalPages ?? data.TotalPages ?? 0),
	};
};

export const getAuditLogs = (params?: AuditLogQueryParams) => {
	return apiClient
		.get("/api/AuditLog", { params })
		.then((res) => normalizePagedAuditLogs(res.data, params?.page ?? 1, params?.pageSize ?? 20));
};
