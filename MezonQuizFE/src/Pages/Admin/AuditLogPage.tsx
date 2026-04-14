import { useCallback, useEffect, useState } from "react";
import {
    Box,
    Button,
    Chip,
    Pagination,
    Paper,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow,
    TextField,
    Typography,
} from "@mui/material";
import { getAuditLogs } from "../../Api/auditlog.api";
import type { AuditLogItemDto, AuditLogQueryParams } from "../../Interface/auditlog.dto";

const formatDate = (isoDate: string) => {
    if (!isoDate) {
        return "-";
    }

    const parsed = new Date(isoDate);
    if (Number.isNaN(parsed.getTime())) {
        return "-";
    }

    return new Intl.DateTimeFormat("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    }).format(parsed);
};

const getStatusChipColor = (status: string): "default" | "warning" | "success" | "error" | "info" => {
    const normalized = status.toLowerCase();
    if (["published", "active", "finished", "success"].includes(normalized)) {
        return "success";
    }

    if (["draft", "paused", "waiting", "pending"].includes(normalized)) {
        return "warning";
    }

    if (["cancelled", "archived", "failed", "error"].includes(normalized)) {
        return "error";
    }

    return "info";
};

const AuditLogPage = () => {
    const [recentActivities, setRecentActivities] = useState<AuditLogItemDto[]>([]);
    const [page, setPage] = useState(1);
    const [pageSize] = useState(20);
    const [totalPages, setTotalPages] = useState(0);
    const [totalCount, setTotalCount] = useState(0);

    const [filters, setFilters] = useState<Omit<AuditLogQueryParams, "page" | "pageSize">>({
        action: "",
        resourceType: "",
        user: "",
        status: "",
        fromDate: "",
        toDate: "",
    });

    const [appliedFilters, setAppliedFilters] = useState(filters);

    const loadAuditLogs = useCallback(async () => {
        const data = await getAuditLogs({
            page,
            pageSize,
            action: appliedFilters.action || undefined,
            resourceType: appliedFilters.resourceType || undefined,
            user: appliedFilters.user || undefined,
            status: appliedFilters.status || undefined,
            fromDate: appliedFilters.fromDate || undefined,
            toDate: appliedFilters.toDate || undefined,
        });

        setRecentActivities(data.items);
        setTotalCount(data.totalCount);
        setTotalPages(data.totalPages);
    }, [appliedFilters, page, pageSize]);

    useEffect(() => {
        let isMounted = true;

        const run = async () => {
            try {
                await loadAuditLogs();
            } catch {
                if (isMounted) {
                    setRecentActivities([]);
                    setTotalCount(0);
                    setTotalPages(0);
                }
            }
        };

        void run();

        return () => {
            isMounted = false;
        };
    }, [loadAuditLogs]);

    const onApplyFilters = () => {
        setPage(1);
        setAppliedFilters(filters);
    };

    const onResetFilters = () => {
        const reset = {
            action: "",
            resourceType: "",
            user: "",
            status: "",
            fromDate: "",
            toDate: "",
        };
        setFilters(reset);
        setPage(1);
        setAppliedFilters(reset);
    };

    return (
    <Paper variant="outlined" sx={{ p: 2.25, boxShadow: "none" }}>
        <Typography variant="h6" fontWeight={700} mb={1.25}>
            Recent Activities
        </Typography>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.25} mb={1.5}>
            <TextField
                size="small"
                label="Action"
                value={filters.action}
                onChange={(event) => setFilters((prev) => ({ ...prev, action: event.target.value }))}
            />
            <TextField
                size="small"
                label="Resource"
                value={filters.resourceType}
                onChange={(event) => setFilters((prev) => ({ ...prev, resourceType: event.target.value }))}
            />
            <TextField
                size="small"
                label="User"
                value={filters.user}
                onChange={(event) => setFilters((prev) => ({ ...prev, user: event.target.value }))}
            />
            <TextField
                size="small"
                label="Status"
                value={filters.status}
                onChange={(event) => setFilters((prev) => ({ ...prev, status: event.target.value }))}
            />
        </Stack>
        <Stack direction={{ xs: "column", md: "row" }} spacing={1.25} mb={1.5} alignItems={{ md: "center" }}>
            <TextField
                size="small"
                type="date"
                label="From"
                InputLabelProps={{ shrink: true }}
                value={filters.fromDate}
                onChange={(event) => setFilters((prev) => ({ ...prev, fromDate: event.target.value }))}
            />
            <TextField
                size="small"
                type="date"
                label="To"
                InputLabelProps={{ shrink: true }}
                value={filters.toDate}
                onChange={(event) => setFilters((prev) => ({ ...prev, toDate: event.target.value }))}
            />
            <Stack direction="row" spacing={1}>
                <Button variant="contained" onClick={onApplyFilters}>Apply</Button>
                <Button variant="outlined" onClick={onResetFilters}>Reset</Button>
            </Stack>
        </Stack>
        <Table>
            <TableHead>
                <TableRow>
                    <TableCell>Resource Type</TableCell>
                    <TableCell>Action</TableCell>
                    <TableCell>User</TableCell>
                    <TableCell>Ip Address</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Created At</TableCell>
                </TableRow>
            </TableHead>
            <TableBody>
                {recentActivities.map((activity, index) => {
                    const details = activity.details;
                    const userDisplayName = activity.userDisplayName;
                    const resourceType = activity.resourceType || "audit";
                    const action = activity.action || "-";
                    const ipAddress = activity.ipAddress || "-";
                    const status = details?.status || "Logged";

                    return (
                        <TableRow key={`${resourceType}-${action}-${activity.createdAt}-${index}`}>
                        <TableCell>
                            <Chip
                                size="small"
                                    label={resourceType.toUpperCase()}
                                variant="outlined"
                            />
                        </TableCell>
                        <TableCell>{action}</TableCell>
                        <TableCell>{userDisplayName}</TableCell>
                        <TableCell>{ipAddress}</TableCell>
                        <TableCell>
                            <Chip
                                size="small"
                                    label={status}
                                    color={getStatusChipColor(status)}
                            />
                        </TableCell>
                        <TableCell>{formatDate(activity.createdAt)}</TableCell>
                    </TableRow>
                );
            })}
                {recentActivities.length === 0 ? (
                    <TableRow>
                        <TableCell colSpan={6} align="center">
                            <Typography variant="body2" color="text.secondary">
                                No recent activities.
                            </Typography>
                        </TableCell>
                    </TableRow>
                ) : null}
            </TableBody>
        </Table>
        <Box mt={1.5} display="flex" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
            <Typography variant="body2" color="text.secondary">
                Total: {totalCount}
            </Typography>
            <Pagination
                page={page}
                count={Math.max(totalPages, 1)}
                onChange={(_, value) => setPage(value)}
                shape="rounded"
                color="primary"
            />
        </Box>
    </Paper>
    );
};

export default AuditLogPage;